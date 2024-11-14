using Nextplace.Api.Db;
using Nextplace.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Nextplace.Api.Controllers;

[Tags("Validator APIs")]
[ApiController]
[Route("Validator")]
public class ValidatorController(AppDbContext context, IConfiguration configuration, IMemoryCache cache) : ControllerBase
{
    [HttpPost("Info", Name = "PostValidatorInfo")]
    public async Task<ActionResult> PostValidatorInfo(PostValidatorInfoRequest request)
    {
        var executionInstanceId = Guid.NewGuid().ToString();
        
        try
        {
            if (!HttpContext.CheckRateLimit(cache, configuration, "PostValidatorInfo", out var offendingIpAddress))
            {
                await context.SaveLogEntry("PostValidatorInfo", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
                return StatusCode(429);
            }

            await context.SaveLogEntry("PostValidatorInfo", "Started", "Information", executionInstanceId);
            await context.SaveLogEntry("PostValidatorInfo", "Info: " + JsonConvert.SerializeObject(request), "Information", executionInstanceId);

            var ipAddressList = HttpContext.GetIpAddressesFromHeader(out var ipAddressLog);
             
            var validators = await context.Validator.Where(w => w.Active == true).ToListAsync();
            var matchingValidator = validators.FirstOrDefault(v => ipAddressList.Contains(v.IpAddress));

            await context.SaveLogEntry("PostValidatorInfo", $"IP Addresses: {ipAddressLog}", "Information", executionInstanceId);

            if (matchingValidator == null)
            {
                await context.SaveLogEntry("PostValidatorInfo", "IP address not allowed", "Warning", executionInstanceId);
                await context.SaveLogEntry("PostValidatorInfo", "Completed", "Information", executionInstanceId);

                return StatusCode(403);
            }

            await context.SaveLogEntry("PostValidatorInfo", $"IP address returned for validator {matchingValidator.HotKey} (ID: {matchingValidator.Id})", "Information", executionInstanceId);

            matchingValidator.AppVersion = request.Version;
            matchingValidator.LastUpdateDate = DateTime.UtcNow;

            await context.SaveChangesAsync();

            await context.SaveLogEntry("PostValidatorInfo", "Saving to DB", "Information", executionInstanceId);
     

            await context.SaveLogEntry("PostValidatorInfo", "Completed", "Information", executionInstanceId);
            return Ok();
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("PostValidatorInfo", ex, executionInstanceId);
            return StatusCode(500);
        }
    }
    
    [HttpGet("Info", Name = "GetValidatorInfo")]
    public async Task<ActionResult> GetValidatorInfo()
    {
        var executionInstanceId = Guid.NewGuid().ToString();

        try
        {
            if (!HttpContext.CheckRateLimit(cache, configuration, "GetValidatorInfo", out var offendingIpAddress))
            {
                await context.SaveLogEntry("GetValidatorInfo", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
                return StatusCode(429);
            }

            await context.SaveLogEntry("GetValidatorInfo", "Started", "Information", executionInstanceId);

         
            var validators = await context.Validator.Where(w => w.Active == true).ToListAsync();

            var l = validators.Select(validator => new Models.Validator(validator.HotKey, validator.ColdKey)
                { Version = validator.AppVersion }).ToList();

            await context.SaveLogEntry("GetValidatorInfo", "Completed", "Information", executionInstanceId);
            return Ok(l);
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("GetValidatorInfo", ex, executionInstanceId);
            return StatusCode(500);
        }
    }
}