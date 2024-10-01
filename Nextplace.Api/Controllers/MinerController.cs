using Azure.Identity;
using Nextplace.Api.Db;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using Miner = Nextplace.Api.Models.Miner;
using Microsoft.Extensions.Caching.Memory;

namespace Nextplace.Api.Controllers;

[Tags("Miner APIs")]
[ApiController]
[Route("Miner")]
public class MinerController(AppDbContext context, IConfiguration configuration, IMemoryCache cache) : ControllerBase
{
    [SwaggerOperation(
        "Get miner stats")]
    [HttpGet("Stats", Name = "Stats")]
    public async Task<ActionResult<List<Models.MinerStats>>> GetMinerStats()
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

            var stats = await context.MinerStats.Include(minerRanking => minerRanking.Miner).ToListAsync();

            var l = (from stat in stats
                let minerDbEntry = stat.Miner
                let miner =
                    new Miner(minerDbEntry.HotKey, minerDbEntry.ColdKey, minerDbEntry.CreateDate,
                        minerDbEntry.LastUpdateDate, minerDbEntry.Active)
                select new Models.MinerStats(miner, stat.Ranking, stat.StatType, stat.NumberOfPredictions,
                    stat.CorrectPredictions)).ToList();
            
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
}