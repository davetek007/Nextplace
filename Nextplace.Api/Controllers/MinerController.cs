using Nextplace.Api.Db;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using Miner = Nextplace.Api.Models.Miner;
using Microsoft.Extensions.Caching.Memory;
using Nextplace.Api.Models;

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

            var miners = await context.Miner.Where(m => m.Active).ToListAsync();

            var ranking = 0;
            
            var l = new List<MinerStats>();

            foreach (var miner in miners.OrderByDescending(m => m.Incentive))
            {
                var minerStats = new MinerStats(new Miner(miner.HotKey, miner.ColdKey, miner.CreateDate,
                    miner.LastUpdateDate, miner.Active, miner.Incentive), ++ranking);

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
}