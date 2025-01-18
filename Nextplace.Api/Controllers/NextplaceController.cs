using Nextplace.Api.Db;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Azure.Identity;
using Azure.Storage.Blobs;

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

  [HttpGet("/DownloadData/{id}", Name = "DownloadData")]
  [SwaggerOperation("Download data")]
  public async Task<ActionResult> DownloadData([FromRoute] string id)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, config, "DownloadData", out var offendingIpAddress))
      {
        await context.SaveLogEntry("DownloadData", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("DownloadData", "Started", "Information", executionInstanceId, clientIp);

      var storageAccountUrl = config["BlobStorageAccountUrl"]!;
      var containerName = config["BlobStorageContainerName"]!;

      var userAssignedClientId = config["ManagedIdentityId"];

      var credentialOptions = new DefaultAzureCredentialOptions
      {
        ManagedIdentityClientId = userAssignedClientId
      };

      var blobServiceClient =
          new BlobServiceClient(new Uri(storageAccountUrl), new DefaultAzureCredential(credentialOptions));

      var container = blobServiceClient.GetBlobContainerClient(containerName);

      var blob = container.GetBlobClient(id);
      var blobProperties = await blob.GetPropertiesAsync();
      var contentType = blobProperties.Value.ContentType;

      Response.AppendCorsHeaders();

      await context.SaveLogEntry("DownloadData", "Completed", "Information", executionInstanceId, clientIp);
      return new FileStreamResult(await blob.OpenReadAsync(), contentType)
      {
        EnableRangeProcessing = true
      };
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("DownloadData", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }
}