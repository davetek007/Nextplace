using Nextplace.Api.Db;
using Nextplace.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Miner = Nextplace.Api.Db.Miner;
using PropertyPrediction = Nextplace.Api.Db.PropertyPrediction;
using Microsoft.Extensions.Caching.Memory;
using Swashbuckle.AspNetCore.Annotations;

namespace Nextplace.Api.Controllers;

[Tags("Prediction APIs")]
[ApiController]
[Route("Predictions")]
public class PredictionController(AppDbContext context, IConfiguration configuration, IMemoryCache cache) : ControllerBase
{
  [HttpPost]
  public async Task<ActionResult> PostPredictions(List<PostPredictionRequest> request)
  {
    var executionInstanceId = Guid.NewGuid().ToString();
    var predictionLimit = configuration.GetValue<int>("PostPredictionsLimit");

    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "PostPredictions", out var offendingIpAddress))
      {
        await context.SaveLogEntry("PostPredictions", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("PostPredictions", "Started", "Information", executionInstanceId);
      await context.SaveLogEntry("PostPredictions", "Predictions: " + JsonConvert.SerializeObject(request), "Information", executionInstanceId);

      var ipAddressList = HttpContext.GetIpAddressesFromHeader(out var ipAddressLog);

      var validators = await context.Validator.Where(w => w.Active == true).ToListAsync();
      var matchingValidator = validators.FirstOrDefault(v => ipAddressList.Contains(v.IpAddress));

      await context.SaveLogEntry("PostPredictions", $"IP Addresses: {ipAddressLog}", "Information", executionInstanceId);

      if (HelperExtensions.IsIpWhitelisted(configuration, ipAddressList))
      {
        await context.SaveLogEntry("PostPredictions", "IP address whitelisted", "Information", executionInstanceId);
      }
      else if (matchingValidator == null)
      {
        await context.SaveLogEntry("PostPredictions", "IP address not allowed", "Warning", executionInstanceId);
        await context.SaveLogEntry("PostPredictions", "Completed", "Information", executionInstanceId);

        return StatusCode(403);
      }
      else
      {
        await context.SaveLogEntry("PostPredictions", $"IP address returned for validator {matchingValidator.HotKey} (ID: {matchingValidator.Id})", "Information", executionInstanceId);
      }

      var propertyMissing = 0;
      var propertyValuationMissing = 0;
      var activePropertyValuationPredictions = 0;
      var activePredictions = 0;
      var deleted = 0;
      var inserted = 0;
      var insertedPropertyValuations = 0;
      var predictedSaleDateMissing = 0;

      if (request.Count > predictionLimit)
      {
        await context.SaveLogEntry("PostPredictions", "Too many predictions", "Warning", executionInstanceId);
        await context.SaveLogEntry("PostPredictions", "Completed", "Information", executionInstanceId);
        return CreatedAtAction(nameof(PostPredictions), null, null);
      }

      foreach (var prediction in request)
      {
        if (prediction.NextplaceId.StartsWith("PVR-"))
        {
          var propertyValuation =
              await context.PropertyValuation.Where(p => p.NextplaceId == prediction.NextplaceId)
                  .FirstOrDefaultAsync();

          if (propertyValuation == null)
          {
            propertyValuationMissing++;
            continue;
          }

          var minerId = await GetMinerId(prediction.MinerHotKey, prediction.MinerColdKey, executionInstanceId);

          var hasActivePredictions = await context.PropertyValuationPrediction.Where(p =>
              p.MinerId == minerId && p.PropertyValuationId == propertyValuation.Id && p.Active).AnyAsync(existingEntry =>
              existingEntry.PredictionDate >= prediction.PredictionDate);

          if (hasActivePredictions)
          {
            activePropertyValuationPredictions++;
            continue;
          }

          var dbEntry = new PropertyValuationPrediction
          {
            MinerId = minerId,
            PredictedSalePrice = prediction.PredictedSalePrice,
            PredictionDate = prediction.PredictionDate,
            PropertyValuationId = propertyValuation.Id,
            CreateDate = DateTime.UtcNow,
            Active = true,
            LastUpdateDate = DateTime.UtcNow
          };

          if (prediction.PredictionScore.HasValue)
          {
            dbEntry.PredictionScore = prediction.PredictionScore.Value;
          }

          if (matchingValidator != null)
          {
            dbEntry.ValidatorId = matchingValidator.Id;
          }

          insertedPropertyValuations++;

          context.PropertyValuationPrediction.Add(dbEntry); 
        }
        else
        {
          var property =
              await context.Property.Where(p => p.NextplaceId == prediction.NextplaceId)
                  .OrderByDescending(p => p.ListingDate).FirstOrDefaultAsync();

          if (property == null)
          {
            propertyMissing++;
            continue;
          }

          if (!prediction.PredictedSaleDate.HasValue)
          {
            predictedSaleDateMissing++;
            continue;
          }

          var minerId = await GetMinerId(prediction.MinerHotKey, prediction.MinerColdKey, executionInstanceId);

          var dbEntry = new PropertyPrediction
          {
            MinerId = minerId,
            PredictedSaleDate = prediction.PredictedSaleDate!.Value,
            PredictedSalePrice = prediction.PredictedSalePrice,
            PredictionDate = prediction.PredictionDate,
            PropertyId = property.Id,
            CreateDate = DateTime.UtcNow,
            Active = true,
            LastUpdateDate = DateTime.UtcNow
          };

          if (prediction.PredictionScore.HasValue)
          {
            dbEntry.PredictionScore = prediction.PredictionScore.Value;
          }

          if (matchingValidator != null)
          {
            dbEntry.ValidatorId = matchingValidator.Id;
          }

          inserted++;

          context.PropertyPrediction.Add(dbEntry);
        }
      }
      
      await context.SaveChangesAsync();
      
      await context.SaveLogEntry("PostPredictions",
          $"Properties: Inserted {inserted}, Deleted {deleted}, Properties missing {propertyMissing}, Active predictions {activePredictions}, Predicted Sale Date missing {predictedSaleDateMissing}", "Information", executionInstanceId);

      await context.SaveLogEntry("PostPredictions",
          $"Property Valuations: Inserted {insertedPropertyValuations}, Property valuations missing {propertyValuationMissing}, Active predictions {activePropertyValuationPredictions}", "Information", executionInstanceId);

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

  private async Task<long> GetMinerId(string hotKey, string coldKey, string executionInstanceId)
  {
    var cacheKey = "Miner" + hotKey + coldKey;
    if (cache.TryGetValue(cacheKey, out var cachedData))
    {
      var id = (long)cachedData!;
      return id;
    }
    
    var miner = await context.Miner.FirstOrDefaultAsync(m => m.HotKey == hotKey) ??
                await AddMiner(hotKey, coldKey, executionInstanceId);


    cache.Set(cacheKey, miner.Id, TimeSpan.FromDays(1));
    return miner.Id;
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
      Incentive = 0,
      Uid = 0
    };

    context.Miner.Add(miner);

    await context.SaveChangesAsync();

    await context.SaveLogEntry("PostPredictions", $"Miner {hotKey}, {coldKey} added. ID {miner.Id}", "Information", executionInstanceId);

    return miner;
  }

  [HttpGet("Search", Name = "SearchPropertyPredictions")]
  [SwaggerOperation("Search for property predictions")]
  public async Task<ActionResult<List<Models.PropertyPrediction>>> Search([FromQuery] PredictionFilter filter)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "SearchPropertyPredictions", out var offendingIpAddress))
      {
        await context.SaveLogEntry("SearchPropertyPredictions", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("SearchPropertyPredictions", "Started", "Information", executionInstanceId);
      await context.SaveLogEntry("SearchPropertyPredictions", "Filter: " + JsonConvert.SerializeObject(filter), "Information", executionInstanceId);

      filter.ItemsPerPage = Math.Clamp(filter.ItemsPerPage, 1, 200);

      var query = context.PropertyPrediction
          .Include(tgp => tgp.Property)
          .Include(tgp => tgp.Miner)
          .Where(tgp => tgp.Active);

      query = ApplyDateFilters(query, filter);
      query = ApplyPropertyIdFilter(query, filter);
      query = ApplyMinerFilter(query, filter);
      query = ApplySortOrder(query, filter.SortOrder);

      var totalCount = await query.CountAsync();
      Response.Headers.Append("Bettensor-Search-Total-Count", totalCount.ToString());

      Response.AppendCorsHeaders();

      await context.SaveLogEntry("SearchPropertyPredictions", $"{totalCount} predictions found", "Information", executionInstanceId);

      query = query.Skip(filter.PageIndex * filter.ItemsPerPage).Take(filter.ItemsPerPage);

      var propertyPredictions = await GetPropertyPredictions(query);

      await context.SaveLogEntry("SearchPropertyPredictions", "Completed", "Information", executionInstanceId);
      return Ok(propertyPredictions);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("SearchPropertyPredictions", ex, executionInstanceId);
      return StatusCode(500);
    }
  }

  private static IQueryable<PropertyPrediction> ApplyDateFilters(IQueryable<PropertyPrediction> query, PredictionFilter filter)
  {
    if (filter.StartDate.HasValue)
    {
      query = query.Where(tgp => tgp.PredictionDate >= filter.StartDate.Value);
    }

    if (filter.EndDate.HasValue)
    {
      query = query.Where(tgp => tgp.PredictionDate <= filter.EndDate.Value);
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

  private static IQueryable<PropertyPrediction> ApplyPropertyIdFilter(IQueryable<PropertyPrediction> query, PredictionFilter filter)
  {
    if (filter.PropertyId.HasValue)
    {
      query = query.Where(tgp => tgp.PropertyId == filter.PropertyId.Value);
    }

    return query;
  }

  private static IQueryable<PropertyPrediction> ApplySortOrder(IQueryable<PropertyPrediction> query, string sortOrder)
  {
    return sortOrder switch
    {
      "date_asc" => query.OrderBy(tgp => tgp.PredictionDate),
      "date_desc" => query.OrderByDescending(tgp => tgp.PredictionDate),
      _ => query.OrderByDescending(tgp => tgp.PredictionDate)
    };
  }

  private static async Task<List<Models.PropertyPrediction>> GetPropertyPredictions(IQueryable<PropertyPrediction> query)
  {
    var propertyPredictions = new List<Models.PropertyPrediction>();

    var results = await query.ToListAsync();

    foreach (var data in results)
    {
      var teamGamePrediction = new Models.PropertyPrediction(
          data.Miner.HotKey,
          data.Miner.ColdKey,
          data.PredictionDate,
          data.PredictedSalePrice,
          data.PredictedSaleDate)
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
          data.Property.CreateDate,
          data.Property.LastUpdateDate,
          data.Property.Active,
          data.Property.Country)
      };

      propertyPredictions.Add(teamGamePrediction);
    }

    return propertyPredictions;
  }
}