using Nextplace.Api.Db;
using Nextplace.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Nextplace.Api.Controllers;

[Tags("Validator APIs")]
[ApiController]
[Route("Validator")]
public class ValidatorController(AppDbContext context) : ControllerBase
{
  [HttpPost("Info", Name = "PostValidatorInfo")]
  public async Task<ActionResult> PostValidatorInfo(PostValidatorInfoRequest request)
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

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
    
    return Ok();
  }

  [HttpGet("Info", Name = "GetValidatorInfo")]
  public async Task<ActionResult> GetValidatorInfo()
  {
    var validators = await context.Validator.Where(w => w.Active == true).ToListAsync();

    var l = validators.Select(validator => new Models.Validator(validator.HotKey, validator.ColdKey)
    { Version = validator.AppVersion }).ToList();

    return Ok(l);
  }
}