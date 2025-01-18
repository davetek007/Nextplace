using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Nextplace.Api.Helpers;

internal class BlobHelper(IConfiguration configuration)
{
  internal async Task SaveToBlobStorage(byte[] fileBytes, string fileName)
  {
    var storageAccountUrl = configuration["BlobStorageAccountUrl"]!;
    var containerName = configuration["BlobStorageContainerName"]!;

    var userAssignedClientId = configuration["ManagedIdentityId"];

    var credentialOptions = new DefaultAzureCredentialOptions
    {
      ManagedIdentityClientId = userAssignedClientId
    };

    var blobServiceClient =
      new BlobServiceClient(new Uri(storageAccountUrl), new DefaultAzureCredential(credentialOptions));

    var container = blobServiceClient.GetBlobContainerClient(containerName);

    var blob = container.GetBlobClient(fileName);
    using var stream = new MemoryStream(fileBytes);

    var headers = new BlobHttpHeaders
    {
      ContentType = "image/png"
    };

    await blob.UploadAsync(stream, new BlobUploadOptions
    {
      HttpHeaders = headers
    });
  }
}