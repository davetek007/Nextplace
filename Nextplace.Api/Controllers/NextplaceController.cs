using Nextplace.Api.Db;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;

namespace Nextplace.Api.Controllers;

[Tags("Nextplace APIs")]
[ApiController]
[Route("Nextplace")]
public class NextplaceController(AppDbContext context, IConfiguration config, IMemoryCache cache) : ControllerBase
{
    [HttpGet("/Version", Name = "GetVersion")]
    [SwaggerOperation("Get product version")]
    public async Task<ActionResult> GetVersion()
    {
        var executionInstanceId = Guid.NewGuid().ToString();
        try
        {
            if (!HttpContext.CheckRateLimit(cache, config, "GetVersion", out var offendingIpAddress))
            {
                await context.SaveLogEntry("GetVersion", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
                return StatusCode(429);
            }
            
            await context.SaveLogEntry("GetVersion", "Started", "Information", executionInstanceId);

            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

            Response.AppendCorsHeaders();

            await context.SaveLogEntry("GetVersion", "Completed", "Information", executionInstanceId);
            return Ok(new { version });
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("GetVersion", ex, executionInstanceId);
            return StatusCode(500);
        }
    }
}