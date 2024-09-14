using Nextplace.Api.Db;
using Nextplace.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using PropertyPrediction = Nextplace.Api.Models.PropertyPrediction;
using Property = Nextplace.Api.Models.Property;
using PropertyContext = Nextplace.Api.Db.Property;

namespace Nextplace.Api.Controllers;

[Tags("Property APIs")]
[ApiController]
[Route("Properties")]
public class PropertyController(AppDbContext context) : ControllerBase
{
    [HttpGet("Search", Name = "SearchProperties")]
    [SwaggerOperation("Search for properties")]
    public async Task<List<Property>> Search([FromQuery] PropertyFilter filter)
    {
        var executionInstanceId = Guid.NewGuid().ToString();

        try
        {
            await context.SaveLogEntry("SearchProperties", "Started", "Information", executionInstanceId);
            await context.SaveLogEntry("SearchProperties", "Filter: " + JsonConvert.SerializeObject(filter), "Information", executionInstanceId);

            filter.ItemsPerPage = Math.Clamp(filter.ItemsPerPage, 1, 500);

            var query = context.Property
                .Include(tg => tg.Predictions)
                .AsQueryable();

            query = ApplyDateFilters(query, filter);
            query = ApplyStringFilters(query, filter);
            query = ApplyMarketFilter(query, filter);
            query = ApplyCoordinateFilters(query, filter);
            query = ApplyAwaitingResultFilter(query, filter);
            query = ApplyMinPredictionsFilter(query, filter);
            query = ApplySortOrder(query, filter.SortOrder);

            var totalCount = await query.CountAsync();
            Response.Headers.Append("Nextplace-Search-Total-Count", totalCount.ToString());

            Response.AppendCorsHeaders();

            await context.SaveLogEntry("SearchProperties", $"{totalCount} properties found", "Information", executionInstanceId);

            query = query.Skip(filter.PageIndex * filter.ItemsPerPage).Take(filter.ItemsPerPage);

            var properties = await GetPropertiesWithPredictionStats(query);

            await context.SaveLogEntry("SearchProperties", "Completed", "Information", executionInstanceId);
            return properties;
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("SearchProperties", ex, executionInstanceId);
            return null!;
        }
    }

    [HttpGet("{propertyId}", Name = "GetProperty")]
    [SwaggerOperation("Get for property by ID")]
    public async Task<IActionResult> Get([SwaggerParameter("Property ID", Required = true)][FromRoute] long propertyId)
    {
        var executionInstanceId = Guid.NewGuid().ToString();

        try
        {
            await context.SaveLogEntry("GetProperty", "Started", "Information", executionInstanceId);
            var property = await context.Property.Include(tg => tg.Predictions)!
                .ThenInclude(propertyPrediction => propertyPrediction.Miner)
                .FirstOrDefaultAsync(tg => tg.Id == propertyId);

            if (property == null)
            {
                await context.SaveLogEntry("GetProperty", "No data", "Warning", executionInstanceId);
                await context.SaveLogEntry("GetProperty", "Completed", "Information", executionInstanceId);
                return NotFound();
            }
            var tg = new Property(
                property.Id,
                property.PropertyId,
                property.NextplaceId,
                property.ListingId,
                property.Longitude,
                property.Latitude,
                property.Market,
                property.City,
                property.State,
                property.ZipCode,
                property.Address,
                property.ListingDate,
                property.ListingPrice,
                property.NumberOfBeds,
                property.NumberOfBaths,
                property.SquareFeet,
                property.LotSize,
                property.YearBuilt,
                property.PropertyType,
                property.LastSaleDate,
                property.HoaDues,
                property.SaleDate,
                property.SalePrice,
                property.CreateDate,
                property.LastUpdateDate,
                property.Active)
            {
                Predictions = new List<PropertyPrediction>()
            };


            if (property.Predictions != null)
            {
                foreach (var prediction in property.Predictions.Where(p => p.Active))
                {
                    var tgp = new PropertyPrediction(prediction.Miner.HotKey,
                        prediction.Miner.ColdKey, prediction.PredictionDate, prediction.PredictedSalePrice,
                        prediction.PredictedSaleDate, null!);

                    tg.Predictions.Add(tgp);
                }
            }

            Response.AppendCorsHeaders();
            
            await context.SaveLogEntry("GetProperty", "Completed", "Information", executionInstanceId);
            return Ok(tg);
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("GetProperty", ex, executionInstanceId);
            return StatusCode(500);
        }
    }

    [HttpGet("Cities/Trending", Name = "GetTrendingCities")]
    [SwaggerOperation("Get trending cities")]
    public async Task<List<TrendingCity>> GetTrendingCities()
    {
        var executionInstanceId = Guid.NewGuid().ToString();

        try
        {
            await context.SaveLogEntry("GetTrendingCities", "Started", "Information", executionInstanceId);
            const int resultCount = 200;

            var properties = await context.Property
                .Where(tg => tg.Predictions != null)
                .Include(tg => tg.Predictions)
                .ToListAsync();

            var trendingCities = properties.Where(p => p.City != null)
                .GroupBy(tg => new { tg.City })
                .Select(g => new TrendingCity(
                    g.Key.City!,
                    g.Sum(tg => tg.Predictions!.Count(p => p.Active))
                ))
                .OrderByDescending(g => g.NumberOfPredictions)
                .Take(resultCount)
                .ToList();
            
            Response.AppendCorsHeaders();
            
            await context.SaveLogEntry("GetTrendingCities", "Completed", "Information", executionInstanceId);
            return trendingCities;
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("GetTrendingCities", ex, executionInstanceId);
            return null!;
        }
    }


    [HttpGet("Markets/Trending", Name = "GetTrendingMarkets")]
    [SwaggerOperation("Get trending markets")]
    public async Task<List<TrendingMarket>> GetTrendingMarkets()
    {
        var executionInstanceId = Guid.NewGuid().ToString();

        try
        {
            await context.SaveLogEntry("GetTrendingMarkets", "Started", "Information", executionInstanceId);
            const int resultCount = 200;

            var properties = await context.Property
                .Where(tg => tg.Predictions != null)
                .Include(tg => tg.Predictions)
                .ToListAsync();

            var trendingMarkets = properties
                .GroupBy(tg => new { tg.Market })
                .Select(g => new TrendingMarket(
                    g.Key.Market,
                    g.Sum(tg => tg.Predictions!.Count(p => p.Active))
                ))
                .OrderByDescending(g => g.NumberOfPredictions)
                .Take(resultCount)
                .ToList();

            Response.AppendCorsHeaders();

            await context.SaveLogEntry("GetTrendingMarkets", "Completed", "Information", executionInstanceId);
            return trendingMarkets;
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("GetTrendingMarkets", ex, executionInstanceId);
            return null!;
        }
    }

    private static IQueryable<PropertyContext> ApplyDateFilters(IQueryable<PropertyContext> query, PropertyFilter filter)
    {
        if (filter.ListingStartDate.HasValue)
        {
            query = query.Where(tg => tg.ListingDate >= filter.ListingStartDate.Value);
        }

        if (filter.ListingEndDate.HasValue)
        {
            query = query.Where(tg => tg.ListingDate <= filter.ListingEndDate.Value);
        }

        if (filter.LastUpdateDate.HasValue)
        {
            query = query.Where(tg => tg.LastUpdateDate > filter.LastUpdateDate.Value);
        }

        return query;
    }

    private static IQueryable<PropertyContext> ApplyStringFilters(IQueryable<PropertyContext> query, PropertyFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.FilterString))
        {
            query = query.Where(tgp =>
                tgp.City != null && tgp.City.Contains(filter.FilterString) ||
                tgp.State != null && tgp.State.Contains(filter.FilterString) ||
                tgp.ZipCode != null && tgp.ZipCode.Contains(filter.FilterString) ||
                tgp.Address != null && tgp.Address.Contains(filter.FilterString));
        }

        return query;
    }

    private static IQueryable<PropertyContext> ApplyMarketFilter(IQueryable<PropertyContext> query, PropertyFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Market))
        {
            query = query.Where(tgp =>
                tgp.Market == filter.Market);
        }

        return query;
    }

    private static IQueryable<PropertyContext> ApplyCoordinateFilters(IQueryable<PropertyContext> query, PropertyFilter filter)
    {
        if (filter is { PropertyMinLatitude: not null, PropertyMaxLatitude: not null })
        {
            query = query.Where(tgp => tgp.Latitude >= filter.PropertyMinLatitude && tgp.Latitude <= filter.PropertyMaxLatitude);
        } 

        if (filter is { PropertyMinLongitude: not null, PropertyMaxLongitude: not null })
        {
            query = query.Where(tgp => tgp.Longitude >= filter.PropertyMinLongitude  && tgp.Longitude <= filter.PropertyMaxLongitude);
        }

        return query;
    }

    private static IQueryable<PropertyContext> ApplyAwaitingResultFilter(IQueryable<PropertyContext> query, PropertyFilter filter)
    {
        if (filter.AwaitingResult.HasValue)
        {
            query = filter.AwaitingResult.Value
                ? query.Where(tg => tg.SaleDate == null)
                : query.Where(tg => tg.SaleDate != null);
        }

        return query;
    }

    private static IQueryable<PropertyContext> ApplyMinPredictionsFilter(IQueryable<PropertyContext> query, PropertyFilter filter)
    {
        if (filter.MinPredictions.HasValue)
        {
            var minPredictions = Math.Clamp(filter.MinPredictions.Value, 0, 50);

            query = query.Where(tg =>
                tg.Predictions!.Count(p => p.Active) >= minPredictions);
        }
        
        return query;
    }

    private static IQueryable<PropertyContext> ApplySortOrder(IQueryable<PropertyContext> query, string sortOrder)
    {
        return sortOrder switch
        {
            "id_asc" => query.OrderBy(tg => tg.Id),
            "id_desc" => query.OrderByDescending(tg => tg.Id),
            "listingdate_asc" => query.OrderBy(tg => tg.ListingDate),
            "listingdate_desc" => query.OrderByDescending(tg => tg.ListingDate),
            "longitude_asc" => query.OrderBy(tgp => tgp.Longitude),
            "longitude_desc" => query.OrderByDescending(tgp => tgp.Longitude),
            "latitude_asc" => query.OrderBy(tgp => tgp.Latitude),
            "latitude_desc" => query.OrderByDescending(tgp => tgp.Latitude),
            "market_asc" => query.OrderBy(tgp => tgp.Market),
            "market_desc" => query.OrderByDescending(tgp => tgp.Market),
            "predictions_asc" => query.OrderBy(tg => tg.Predictions!.Count(p => p.Active)),
            "predictions_desc" => query.OrderByDescending(tg => tg.Predictions!.Count(p => p.Active)),
            _ => query.OrderByDescending(tg => tg.ListingDate)
        };
    }

    private static async Task<List<Property>> GetPropertiesWithPredictionStats(IQueryable<PropertyContext> query)
    {
        var properties = new List<Property>();

        var results = await query.ToListAsync();

        foreach (var data in results)
        {
            properties.Add(new Property(
                data.Id,
                data.PropertyId,
                data.NextplaceId,
                data.ListingId,
                data.Longitude,
                data.Latitude,
                data.Market,
                data.City,
                data.State,
                data.ZipCode,
                data.Address,
                data.ListingDate,
                data.ListingPrice,
                data.NumberOfBeds,
                data.NumberOfBaths,
                data.SquareFeet,
                data.LotSize,
                data.YearBuilt,
                data.PropertyType,
                data.LastSaleDate,
                data.HoaDues,
                data.SaleDate,
                data.SalePrice,
                data.CreateDate,
                data.LastUpdateDate,
                data.Active));

        }

        return properties;
    }
}