using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;
using Azure.Identity;
using Azure.Storage.Blobs;
using Newtonsoft.Json;
using Nextplace.Api.Db;
using Nextplace.Api.Helpers;

namespace Nextplace.Api.Controllers;

[Tags("Nextplace APIs")]
[ApiController]
[Route("Nextplace")]
public class NextplaceController(AppDbContext context, IConfiguration config) : ControllerBase
{
  [HttpGet("/Version", Name = "GetVersion")]
  [SwaggerOperation("Get product version")]
  public ActionResult GetVersion()
  {
    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    return Ok(new { version });
  }

  [HttpGet("/HousecoinPrice", Name = "GetHouseCoinData")]
  [SwaggerOperation("Get the current data of HouseCoin (HOUSE) in USD")]
  public async Task<ActionResult> GetHouseCoinPrice()
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;
    await context.SaveLogEntry("GetHouseCoinPrice", "Initiated", "Information", executionInstanceId);

    var apiKey = await new AkvHelper(config).GetSecretAsync("CoinMarketCapApiKey");

    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", apiKey);
    httpClient.DefaultRequestHeaders.Add("Accepts", "application/json");

    var url = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/quotes/latest?symbol=HOUSE&convert=USD";

    try
    {
      var response = await httpClient.GetAsync(url);
      response.EnsureSuccessStatusCode();
      var json = await response.Content.ReadAsStringAsync(); 

      await context.SaveLogEntry("GetHouseCoinPrice", "Successfully fetched CoinMarketCap data", "Information", executionInstanceId);

      return Content(json, "application/json");
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("GetHouseCoinPrice", $"Error: {ex.Message}", "Error", executionInstanceId);
      return StatusCode(500, "Failed to retrieve HouseCoin price.");
    }
  }

  [HttpGet("/DownloadData/{id}", Name = "DownloadData")]
  [SwaggerOperation("Download data")]
  public async Task<ActionResult> DownloadData([FromRoute] string id)
  {
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

    return new FileStreamResult(await blob.OpenReadAsync(), contentType)
    {
      EnableRangeProcessing = true
    };
  }
}