using Nextplace.Api.Db;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using Miner = Nextplace.Api.Models.Miner;
using Microsoft.Extensions.Caching.Memory;
using Nextplace.Api.Models;
using Newtonsoft.Json;

namespace Nextplace.Api.Controllers;

[Tags("Miner APIs")]
[ApiController]
[Route("Miner")]
public class MinerController(AppDbContext context, IConfiguration configuration, IMemoryCache cache) : ControllerBase
{
    [SwaggerOperation(
        "Get miner stats")]
    [HttpGet("Stats", Name = "Stats")]
    public async Task<ActionResult<List<MinerStats>>> GetMinerStats()
    {
        var executionInstanceId = Guid.NewGuid().ToString();

        try
        {
            if (!HttpContext.CheckRateLimit(cache, configuration, "GetMinerStats", out var offendingIpAddress))
            {
                await context.SaveLogEntry("GetMinerStats", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
                return StatusCode(429);
            }

            await context.SaveLogEntry("GetMinerStats", "Started", "Information", executionInstanceId);

            var miners = context.Miner.Include(m => m.Scores).Where(m => m.Active);

            var ranking = 0;
            
            var l = new List<MinerStats>();

            foreach (var dbEntry in miners.OrderByDescending(m => m.Incentive))
            {
                var scores = dbEntry.Scores!.Where(s => s.Active).ToList();

                double? minScore = null;
                double? avgScore = null;
                double? maxScore = null;
                var numScores = 0;
                var numPredictions = 0;
                DateTime? scoreGenerationDate = null;
                
                if (scores.Any())
                {
                    minScore = scores.Min(s => s.Score);
                    avgScore = scores.Average(s => s.Score);
                    maxScore = scores.Max(s => s.Score);
                    numScores = scores.Count;
                    numPredictions = scores.Max(s => s.NumPredictions);
                    scoreGenerationDate = scores.Max(s => s.ScoreGenerationDate);
                }

                var miner = new Miner(
                    dbEntry.HotKey,
                    dbEntry.ColdKey,
                    dbEntry.CreateDate,
                    dbEntry.LastUpdateDate,
                    dbEntry.Active,
                    dbEntry.Incentive, 
                    dbEntry.Uid)
                {
                    AvgScore = avgScore, MaxScore = maxScore, MinScore = minScore, NumPredictions = numPredictions,
                    NumScores = numScores, ScoreGenerationDate = scoreGenerationDate
                };

                var minerStats = new MinerStats(miner, ++ranking);
                
                l.Add(minerStats);
            }

            Response.AppendCorsHeaders();

            await context.SaveLogEntry("GetMinerStats", "Completed", "Information", executionInstanceId);
            return Ok(l);
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("GetMinerStats", ex, executionInstanceId);
            return StatusCode(500);
        }
    }
   
    [SwaggerOperation(
        "Post miner scores")]
    [HttpPost("Scores", Name = "Scores")]
    public async Task<ActionResult> PostMinerScores(List<PostMinerScoreRequest> request)
    {
        var executionInstanceId = Guid.NewGuid().ToString();

        try
        {
            if (!HttpContext.CheckRateLimit(cache, configuration, "PostMinerScores", out var offendingIpAddress))
            {
                await context.SaveLogEntry("PostMinerScores", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
                return StatusCode(429);
            }

            await context.SaveLogEntry("PostMinerScores", "Started", "Information", executionInstanceId);
            await context.SaveLogEntry("PostMinerScores", "Scores: " + JsonConvert.SerializeObject(request), "Information", executionInstanceId);

            var ipAddressList = HttpContext.GetIpAddressesFromHeader(out var ipAddressLog);

            var validators = await context.Validator.Where(w => w.Active == true).ToListAsync();
            var matchingValidator = validators.FirstOrDefault(v => ipAddressList.Contains(v.IpAddress));

            await context.SaveLogEntry("PostMinerScores", $"IP Addresses: {ipAddressLog}", "Information", executionInstanceId);

            if (HelperExtensions.IsIpWhitelisted(configuration, ipAddressList))
            {
                await context.SaveLogEntry("PostMinerScores", "IP address whitelisted", "Information", executionInstanceId);
            }
            else if (matchingValidator == null)
            {
                await context.SaveLogEntry("PostMinerScores", "IP address not allowed", "Warning", executionInstanceId);
                await context.SaveLogEntry("PostMinerScores", "Completed", "Information", executionInstanceId);

                return StatusCode(403);
            }
            else
            {
                await context.SaveLogEntry("PostMinerScores", $"IP address returned for validator {matchingValidator.HotKey} (ID: {matchingValidator.Id})", "Information", executionInstanceId);
            }

            var deleted = 0;
            var inserted = 0;

            foreach (var minerScore in request)
            {
                var miner = await context.Miner.FirstOrDefaultAsync(m => m.HotKey == minerScore.MinerHotKey) ??
                            await AddMiner(minerScore.MinerHotKey, minerScore.MinerColdKey, executionInstanceId);

                List<MinerScore> existingEntries;
                if (matchingValidator == null)
                {
                    existingEntries = await context.MinerScore.Where(p =>
                        p.MinerId == miner.Id && p.ValidatorId == null && p.Active &&
                        p.ScoreGenerationDate < minerScore.ScoreGenerationDate).ToListAsync();
                }
                else
                {
                    existingEntries = await context.MinerScore.Where(p =>
                        p.MinerId == miner.Id && p.ValidatorId == matchingValidator.Id && p.Active &&
                        p.ScoreGenerationDate < minerScore.ScoreGenerationDate).ToListAsync();
                }

                foreach (var existingEntry in existingEntries)
                {
                    existingEntry.Active = false;
                    existingEntry.LastUpdateDate = DateTime.UtcNow;
                    deleted++;
                }

                var dbEntry = new MinerScore
                {
                    MinerId = miner.Id,
                    Score = minerScore.MinerScore, 
                    NumPredictions = minerScore.NumPredictions, 
                    ScoreGenerationDate = minerScore.ScoreGenerationDate,
                    CreateDate = DateTime.UtcNow,
                    Active = true,
                    LastUpdateDate = DateTime.UtcNow
                };

                if (matchingValidator != null)
                {
                    dbEntry.ValidatorId = matchingValidator.Id;
                }

                inserted++;

                context.MinerScore.Add(dbEntry);
                await context.SaveChangesAsync();
            }

            await context.SaveLogEntry("PostMinerScores", $"Inserted {inserted}, Deleted {deleted}", "Information", executionInstanceId);

            await context.SaveLogEntry("PostMinerScores", "Saving to DB", "Information", executionInstanceId);
            await context.SaveChangesAsync();

            await context.SaveLogEntry("PostMinerScores", "Completed", "Information", executionInstanceId);
            return CreatedAtAction(nameof(PostMinerScores), null, null);
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("PostMinerScores", ex, executionInstanceId);
            return StatusCode(500);
        }
    }


    private async Task<Db.Miner> AddMiner(string hotKey, string coldKey, string executionInstanceId)
    {
        var miner = new Db.Miner
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

        await context.SaveLogEntry("PostMinerScores", $"Miner {hotKey}, {coldKey} added. ID {miner.Id}", "Information", executionInstanceId);

        return miner;
    }
}