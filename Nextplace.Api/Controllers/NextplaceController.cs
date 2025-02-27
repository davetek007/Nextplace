using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace Nextplace.Api.Controllers;

[Tags("Nextplace APIs")]
[ApiController]
[Route("Nextplace")]
public class NextplaceController(IConfiguration config) : ControllerBase
{
  [HttpGet("/Version", Name = "GetVersion")]
  [SwaggerOperation("Get product version")]
  public ActionResult GetVersion()
  {
    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    return Ok(new { version });
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