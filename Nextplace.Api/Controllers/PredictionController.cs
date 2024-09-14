using System.Text;
using Nextplace.Api.Db;
using Nextplace.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using Miner = Nextplace.Api.Db.Miner;
using PropertyPrediction = Nextplace.Api.Db.PropertyPrediction;

namespace Nextplace.Api.Controllers;

[Tags("Prediction APIs")]
[ApiController]
[Route("Predictions")]
public class PredictionController(AppDbContext context, IConfiguration configuration) : ControllerBase
{
    [HttpGet("Search", Name = "SearchPropertyPredictions")]
    [SwaggerOperation("Search for property predictions")]
    public async Task<List<Models.PropertyPrediction>> Search([FromQuery] PredictionFilter filter)
    {
        var executionInstanceId = Guid.NewGuid().ToString();

        try
        {
            await context.SaveLogEntry("SearchPropertyPredictions", "Started", "Information", executionInstanceId);
            await context.SaveLogEntry("SearchPropertyPredictions", "Filter: " + JsonConvert.SerializeObject(filter), "Information", executionInstanceId);

            filter.ItemsPerPage = Math.Clamp(filter.ItemsPerPage, 1, 200);

            var query = context.PropertyPrediction
                .Include(tgp => tgp.Property)
                .Include(tgp => tgp.Miner)
                .ThenInclude(m => m.MinerStats)
                .Where(tgp=>tgp.Active);

            query = ApplyDateFilters(query, filter);
            query = ApplyStringFilters(query, filter);
            query = ApplyMarketFilter(query, filter);
            query = ApplyCoordinateFilters(query, filter);
            query = ApplyPropertyIdFilter(query, filter);
            query = ApplyMinerFilter(query, filter);
            query = ApplyAwaitingResultFilter(query, filter);
            query = ApplySortOrder(query, filter.SortOrder);

            var totalCount = await query.CountAsync();
            Response.Headers.Append("Nextplace-Search-Total-Count", totalCount.ToString());

            Response.AppendCorsHeaders();

            await context.SaveLogEntry("SearchPropertyPredictions", $"{totalCount} predictions found", "Information", executionInstanceId);

            query = query.Skip(filter.PageIndex * filter.ItemsPerPage).Take(filter.ItemsPerPage);

            var propertyPredictions = await GetPropertyPredictionsWithPredictionStats(query);

            await context.SaveLogEntry("SearchPropertyPredictions", "Completed", "Information", executionInstanceId);
            return propertyPredictions;
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("SearchPropertyPredictions", ex, executionInstanceId);
            return null!;
        }
    }
    [HttpPost]
    public async Task<ActionResult> PostPredictions(List<PostPredictionRequest> request)
    {
        var executionInstanceId = Guid.NewGuid().ToString();
        var predictionLimit = configuration.GetValue<int>("PostPredictionsLimit");

        try
        {
            await context.SaveLogEntry("PostPredictions", "Started", "Information", executionInstanceId);
            var ipAddressList = new List<string>();
            var ipAddressLog = new StringBuilder();
            var clientIps = HttpContext.Request.Headers["X-Azure-ClientIP"];
            var socketIps = HttpContext.Request.Headers["X-Azure-SocketIP"];
            var forwardedForIps = HttpContext.Request.Headers["X-Forwarded-For"];
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (clientIps.Count != 0)
            {
                var ipAddresses = clientIps.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
                ipAddressList.AddRange(ipAddresses!);
                ipAddressLog.Append($"X-Azure-ClientIP: {string.Join(',', ipAddresses)}");
            }

            if (socketIps.Count != 0)
            {
                var ipAddresses = socketIps.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
                ipAddressList.AddRange(ipAddresses!);
                if (ipAddressLog.Length != 0) ipAddressLog.Append(", ");
                ipAddressLog.Append($"X-Azure-SocketIP: {string.Join(',', ipAddresses)}");
            }

            if (forwardedForIps.Count != 0)
            {
                var ipAddresses = forwardedForIps.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
                ipAddressList.AddRange(ipAddresses!);
                if (ipAddressLog.Length != 0) ipAddressLog.Append(", ");
                ipAddressLog.Append($"X-Forwarded-For: {string.Join(',', ipAddresses)}");
            }

            if (remoteIp != null)
            {
                ipAddressList.Add(remoteIp);
                if (ipAddressLog.Length != 0) ipAddressLog.Append(", ");
                ipAddressLog.Append($"Remote IP: {remoteIp}");
            }

            var allowedIps = await context.Validator.Where(w => w.Active == true).Select(s => s.IpAddress).Distinct().ToListAsync();

            await context.SaveLogEntry("PostPredictions", $"IP Addresses: {ipAddressLog}", "Information", executionInstanceId);

            if (!ipAddressList.Any(ip => allowedIps.Contains(ip)))
            {
                await context.SaveLogEntry("PostPredictions", "IP address not allowed", "Warning", executionInstanceId);
                await context.SaveLogEntry("PostPredictions", "Completed", "Information", executionInstanceId);

                return StatusCode(403);
            }

            await context.SaveLogEntry("PostPredictions", $"Posting {request.Count} predictions", "Information", executionInstanceId);

            var propertyMissing = 0;
            var badPredictedOutcome = 0;
            var activePredictions = 0;
            var deleted = 0;
            var inserted = 0;

            if (request.Count > predictionLimit)
            {
                await context.SaveLogEntry("PostPredictions", "Too many predictions", "Warning", executionInstanceId);
                await context.SaveLogEntry("PostPredictions", "Completed", "Information", executionInstanceId);
                return CreatedAtAction(nameof(PostPredictions), null, null);
            }

            foreach (var prediction in request)
            {
                var property =
                    await context.Property.FirstOrDefaultAsync(tg => Math.Abs(tg.Latitude - prediction.PropertyLatitude) < 0.0000001 && Math.Abs(tg.Longitude - prediction.PropertyLongitude) < 0.0000001);

                if (property == null)
                {
                    propertyMissing++;
                    continue;
                }

                var miner = await context.Miner.FirstOrDefaultAsync(m => m.HotKey == prediction.MinerHotKey) ??
                            await AddMiner(prediction.MinerHotKey, prediction.MinerColdKey, executionInstanceId);
                 
                var hasActivePredictions = await context.PropertyPrediction.Where(p =>
                    p.MinerId == miner.Id && p.PropertyId == property.Id && p.Active).AnyAsync(existingEntry =>
                    existingEntry.PredictionDate >= prediction.PredictionDate);

                if (hasActivePredictions)
                {
                    activePredictions++;
                    continue;
                }

                var existingEntries = await context.PropertyPrediction.Where(p =>
                    p.MinerId == miner.Id && p.PropertyId == property.Id && p.Active).ToListAsync();

                foreach (var existingEntry in existingEntries)
                {
                    existingEntry.Active = false;
                    existingEntry.LastUpdateDate = DateTime.UtcNow;
                    deleted++;
                }

                var dbEntry = new PropertyPrediction
                {
                    MinerId = miner.Id,
                    PredictedSaleDate = prediction.PredictedSaleDate,
                    PredictedSalePrice = prediction.PredictedSalePrice,
                    PredictionDate = prediction.PredictionDate,
                    PropertyId = property.Id,
                    CreateDate = DateTime.UtcNow,
                    Active = true,
                    LastUpdateDate = DateTime.UtcNow
                };

                inserted++;

                context.PropertyPrediction.Add(dbEntry);
                await context.SaveChangesAsync();
            }

            await context.SaveLogEntry("PostPredictions", $"Inserted {inserted}, Deleted {deleted}, Properties missing {propertyMissing}, Active predictions {activePredictions}, Bad predicted outcome {badPredictedOutcome}", "Information", executionInstanceId);

            await context.SaveLogEntry("PostPredictions", "Saving to DB", "Information", executionInstanceId);
            await context.SaveChangesAsync();

            await context.SaveLogEntry("PostPredictions", "Completed", "Information", executionInstanceId);
            return CreatedAtAction(nameof(PostPredictions), null, null);
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("PostPredictions", ex, executionInstanceId);
            return StatusCode(500);
        }
    }

    private async Task<Miner> AddMiner(string hotKey, string coldKey, string executionInstanceId)
    {
        var miner = new Miner
        {
            HotKey = hotKey,
            ColdKey = coldKey,
            Active = true,
            CreateDate = DateTime.UtcNow,
            LastUpdateDate = DateTime.UtcNow, 
            Incentive = 0
        };

        context.Miner.Add(miner);

        await context.SaveChangesAsync();

        await context.SaveLogEntry("PostPredictions", $"Miner {hotKey}, {coldKey} added. ID {miner.Id}", "Information", executionInstanceId);

        return miner;
    }
    
    private static IQueryable<PropertyPrediction> ApplyDateFilters(IQueryable<PropertyPrediction> query, PredictionFilter filter)
    {
        if (filter.ListingStartDate.HasValue)
        {
            query = query.Where(tgp => tgp.Property.ListingDate >= filter.ListingStartDate.Value);
        }

        if (filter.ListingEndDate.HasValue)
        {
            query = query.Where(tgp => tgp.Property.ListingDate <= filter.ListingEndDate.Value);
        }

        return query;
    }

    private static IQueryable<PropertyPrediction> ApplyMinerFilter(IQueryable<PropertyPrediction> query, PredictionFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.MinerHotKey))
        {
            query = query.Where(tgp => tgp.Miner.HotKey == filter.MinerHotKey);
        }

        if (!string.IsNullOrWhiteSpace(filter.MinerColdKey))
        {
            query = query.Where(tgp => tgp.Miner.ColdKey == filter.MinerColdKey);
        }

        return query;
    }

    private static IQueryable<PropertyPrediction> ApplyMarketFilter(IQueryable<PropertyPrediction> query, PredictionFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Market))
        {
            query = query.Where(tgp =>
                tgp.Property.Market == filter.Market);
        }

        return query;
    }

    private static IQueryable<PropertyPrediction> ApplyCoordinateFilters(IQueryable<PropertyPrediction> query, PredictionFilter filter)
    {
        if (filter is { PropertyMinLatitude: not null, PropertyMaxLatitude: not null })
        {
            query = query.Where(tgp => tgp.Property.Latitude >= filter.PropertyMinLatitude && tgp.Property.Latitude <= filter.PropertyMaxLatitude);
        }

        if (filter is { PropertyMinLongitude: not null, PropertyMaxLongitude: not null })
        {
            query = query.Where(tgp => tgp.Property.Longitude >= filter.PropertyMinLongitude && tgp.Property.Longitude <= filter.PropertyMaxLongitude);
        }

        return query;
    }

    private static IQueryable<PropertyPrediction> ApplyStringFilters(IQueryable<PropertyPrediction> query, PredictionFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.FilterString))
        {
            query = query.Where(tgp =>
                tgp.Property.City!.Contains(filter.FilterString) || tgp.Property.State!.Contains(filter.FilterString) ||
                tgp.Property.ZipCode!.Contains(filter.FilterString) || tgp.Property.Address!.Contains(filter.FilterString));
        }

        return query;
    }

    private static IQueryable<PropertyPrediction> ApplyPropertyIdFilter(IQueryable<PropertyPrediction> query, PredictionFilter filter)
    {
        if (filter.PropertyId.HasValue)
        {
            query = query.Where(tgp => tgp.PropertyId == filter.PropertyId.Value);
        }

        return query;
    }

    private static IQueryable<PropertyPrediction> ApplyAwaitingResultFilter(IQueryable<PropertyPrediction> query, PredictionFilter filter)
    {
        if (filter.AwaitingResult.HasValue)
        {
            query = filter.AwaitingResult.Value
                ? query.Where(tgp => tgp.Property.SaleDate == null)
                : query.Where(tgp => tgp.Property.SaleDate != null);
        }

        return query;
    }
     
    private static IQueryable<PropertyPrediction> ApplySortOrder(IQueryable<PropertyPrediction> query, string sortOrder)
    {
        return sortOrder switch
        {
            "date_asc" => query.OrderBy(tgp => tgp.PredictionDate),
            "date_desc" => query.OrderByDescending(tgp => tgp.PredictionDate),
            "longitude_asc" => query.OrderBy(tgp => tgp.Property.Longitude),
            "longitude_desc" => query.OrderByDescending(tgp => tgp.Property.Longitude),
            "latitude_asc" => query.OrderBy(tgp => tgp.Property.Latitude),
            "latitude_desc" => query.OrderByDescending(tgp => tgp.Property.Latitude),
            "market_asc" => query.OrderBy(tgp => tgp.Property.Market),
            "market_desc" => query.OrderByDescending(tgp => tgp.Property.Market),
            _ => query.OrderByDescending(tgp => tgp.PredictionDate)
        };
    }

    private static async Task<List<Models.PropertyPrediction>> GetPropertyPredictionsWithPredictionStats(IQueryable<PropertyPrediction> query)
    {
        var propertyPredictions = new List<Models.PropertyPrediction>();

        var results = await query.ToListAsync();

        foreach (var data in results)
        {
            var propertyPrediction = new Models.PropertyPrediction(
                data.Miner.HotKey,
                data.Miner.ColdKey, 
                data.PredictionDate,
                data.PredictedSalePrice,
                data.PredictedSaleDate,
                data.Miner.MinerStats!.Select(stat => new Models.MinerStats(
                    new Models.Miner(data.Miner.HotKey, data.Miner.ColdKey, data.Miner.CreateDate, data.Miner.LastUpdateDate, data.Miner.Active), stat.Ranking, stat.StatType, stat.NumberOfPredictions,
                    stat.CorrectPredictions)).ToList())
            {
                Property = new Models.Property(
                    data.Property.Id,
                    data.Property.PropertyId,
                    data.Property.NextplaceId,
                    data.Property.ListingId,
                    data.Property.Longitude,
                    data.Property.Latitude,
                    data.Property.Market,
                    data.Property.City,
                    data.Property.State,
                    data.Property.ZipCode,
                    data.Property.Address,
                    data.Property.ListingDate,
                    data.Property.ListingPrice,
                    data.Property.NumberOfBeds,
                    data.Property.NumberOfBaths,
                    data.Property.SquareFeet,
                    data.Property.LotSize,
                    data.Property.YearBuilt,
                    data.Property.PropertyType,
                    data.Property.LastSaleDate,
                    data.Property.HoaDues,
                    data.Property.SaleDate,
                    data.Property.SalePrice,
                    data.Property.CreateDate!.Value,
                    data.Property.LastUpdateDate!.Value,
                    data.Property.Active!.Value)
            };

            propertyPredictions.Add(propertyPrediction);
        }

        return propertyPredictions;
    }
}