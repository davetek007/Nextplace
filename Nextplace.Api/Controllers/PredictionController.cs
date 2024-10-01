using Nextplace.Api.Db;
using Nextplace.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Miner = Nextplace.Api.Db.Miner;
using PropertyPrediction = Nextplace.Api.Db.PropertyPrediction;
using Microsoft.Extensions.Caching.Memory;

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

            var allowedIps = await context.Validator.Where(w => w.Active == true).Select(s => s.IpAddress).Distinct().ToListAsync();

            await context.SaveLogEntry("PostPredictions", $"IP Addresses: {ipAddressLog}", "Information", executionInstanceId);

            if (HelperExtensions.IsIpWhitelisted(configuration, ipAddressList))
            {
                await context.SaveLogEntry("PostPredictions", "IP address whitelisted", "Information", executionInstanceId);
            }
            else if (!ipAddressList.Any(ip => allowedIps.Contains(ip)))
            {
                await context.SaveLogEntry("PostPredictions", "IP address not allowed", "Warning", executionInstanceId);
                await context.SaveLogEntry("PostPredictions", "Completed", "Information", executionInstanceId);

                return StatusCode(403);
            }
            
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
                    await context.Property.Where(p=>p.NextplaceId == prediction.NextplaceId).OrderByDescending(p=>p.ListingDate).FirstOrDefaultAsync();

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
}