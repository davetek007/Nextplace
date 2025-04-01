using Nextplace.Api.Db;
using Nextplace.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Miner = Nextplace.Api.Db.Miner;
using Microsoft.Extensions.Caching.Memory;
using Nextplace.Api.Helpers;
using Validator = Nextplace.Api.Db.Validator;

namespace Nextplace.Api.Controllers;

[Tags("Prediction APIs")]
[ApiController]
[Route("Predictions")]
public class PredictionController(AppDbContext context, IConfiguration configuration, IMemoryCache cache) : ControllerBase
{
  [HttpPost]
  public async Task<ActionResult> PostPredictions(List<PostPredictionRequest> request)
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

    var predictionLimit = configuration.GetValue<int>("PostPredictionsLimit");

    var ipAddressList = HttpContext.GetIpAddressesFromHeader(out var ipAddressLog);

    var validators = await GetValidators();
    var matchingValidator = validators.FirstOrDefault(v => ipAddressList.Contains(v.IpAddress));

    if (HelperExtensions.IsIpWhitelisted(configuration, ipAddressList, out var whitelistOnly))
    {
      await context.SaveLogEntry("PostPredictions", "Started", "Information", executionInstanceId);
      await context.SaveLogEntry("PostPredictions", $"IP Addresses: {ipAddressLog}", "Information", executionInstanceId);
      await context.SaveLogEntry("PostPredictions", "IP address whitelisted", "Information", executionInstanceId);
    }
    else if (matchingValidator == null)
    {
      await context.SaveLogEntry("PostPredictions", "Started", "Information", executionInstanceId);
      await context.SaveLogEntry("PostPredictions", $"IP Addresses: {ipAddressLog}", "Information", executionInstanceId);
      await context.SaveLogEntry("PostPredictions", "IP address not allowed", "Warning", executionInstanceId);

      return StatusCode(403);
    }
    else
    {
      if (whitelistOnly)
      {
        return CreatedAtAction(nameof(PostPredictions), null, null);
      }

      await context.SaveLogEntry("PostPredictions", "Started", "Information", executionInstanceId);
      await context.SaveLogEntry("PostPredictions", $"IP Addresses: {ipAddressLog}", "Information", executionInstanceId);
      await context.SaveLogEntry("PostPredictions", $"IP address returned for validator {matchingValidator.HotKey} (ID: {matchingValidator.Id})", "Information", executionInstanceId);
    }

    var propertyValuationMissing = 0;
    var activePropertyValuationPredictions = 0;
    var insertedPropertyValuations = 0;

    if (request.Count > predictionLimit)
    {
      await context.SaveLogEntry("PostPredictions", "Too many predictions", "Warning", executionInstanceId);
      return CreatedAtAction(nameof(PostPredictions), null, null);
    }

    var l = new List<PropertyPredictionInfo>();
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
        var propertyId = await GetPropertyId(prediction.NextplaceId);

        if (propertyId == null)
        {
          continue;
        }

        if (!prediction.PredictedSaleDate.HasValue)
        {
          continue;
        }

        var minerId = await GetMinerId(prediction.MinerHotKey, prediction.MinerColdKey, executionInstanceId);

        var pp = new PropertyPredictionInfo
        {
          MinerId = minerId,
          PredictedSaleDate = prediction.PredictedSaleDate!.Value,
          PredictedSalePrice = prediction.PredictedSalePrice,
          PredictionDate = prediction.PredictionDate,
          PropertyId = propertyId.Value,
          CreateDate = DateTime.UtcNow
        };

        if (prediction.PredictionScore.HasValue)
        {
          pp.PredictionScore = prediction.PredictionScore.Value;
        }

        if (matchingValidator != null)
        {
          pp.ValidatorId = matchingValidator.Id;
        }

        l.Add(pp);
      }
    }

    await context.SaveChangesAsync();
    
    await PredictionInserter.InsertPredictionsAsync(context, l, executionInstanceId);

    await context.SaveLogEntry("PostPredictions",
        $"Property Valuations: Inserted {insertedPropertyValuations}, Property valuations missing {propertyValuationMissing}, Active predictions {activePropertyValuationPredictions}", "Information", executionInstanceId);

    await context.SaveLogEntry("PostPredictions", "Saving to DB", "Information", executionInstanceId);
    await context.SaveLogEntry("PostPredictions", "Completed", "Information", executionInstanceId);
    
    await context.SaveChangesAsync();
    
    return CreatedAtAction(nameof(PostPredictions), null, null);
  }

  private async Task<long?> GetPropertyId(string nextplaceId)
  {
    const string cacheKey = "Properties";

    if (cache.TryGetValue(cacheKey, out var cachedData))
    {
      var data = (Dictionary<string, long>)cachedData!;
      if (data.TryGetValue(nextplaceId, out var id))
      {
        return id;
      }

      return null;
    }

    var properties = await context.Property
      .GroupBy(p => p.NextplaceId)
      .Select(g => new
      {
        NextplaceId = g.Key,
        MaxId = g.Max(p => p.Id)
      })
      .ToDictionaryAsync(x => x.NextplaceId, x => x.MaxId);
    
    cache.Set(cacheKey, properties, TimeSpan.FromMinutes(45));

    if (properties.TryGetValue(nextplaceId, out var propertyId))
    {
      return propertyId;
    }

    return null;
  }

  private async Task<List<Validator>> GetValidators()
  {
    const string cacheKey= "Validators";

    if (cache.TryGetValue(cacheKey, out var cachedData))
    {
      var data = (List<Validator>)cachedData!;
      return data;
    }

    var validators = await context.Validator.Where(w => w.Active == true).ToListAsync();
    cache.Set(cacheKey, validators, TimeSpan.FromDays(1));
    return validators;
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
}