using System.Globalization;
using System.IO;
using Azure.Identity;
using Azure.Storage.Blobs;
using CsvHelper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.AppConfig;
using Newtonsoft.Json;
using Nextplace.Functions.Db;

namespace Nextplace.Functions.Functions;

public sealed class CreatePropertyPredictionFile(ILoggerFactory loggerFactory, AppDbContext context, IConfiguration config)
{
  private readonly ILogger _logger = loggerFactory.CreateLogger<CreatePropertyPredictionFile>();

  [Function("CreatePropertyPredictionFile")]
  public async Task Run([TimerTrigger("%CreatePropertyPredictionFileTimerSchedule%")] TimerInfo myTimer)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      _logger.LogInformation($"CreatePropertyPredictionFile executed at: {DateTime.UtcNow}");
      await context.SaveLogEntry("CreatePropertyPredictionFile", "Started", "Information", executionInstanceId);

      var properties = await context.PropertyPredictionStats
        .Include(p => p.Property)
        .Where(p => p.CreateDate > DateTime.UtcNow.AddDays(-1))
        .Select(p => new { p.Top10Predictions, p.Property })
        .ToListAsync();

      var marketDict = new Dictionary<string, List<(Property, double)>>();

      foreach (var propertyInfo in properties)
      {
        if (!marketDict.ContainsKey(propertyInfo.Property.Country + " - " + propertyInfo.Property.Market))
        {
          marketDict.Add(propertyInfo.Property.Country + " - " + propertyInfo.Property.Market, []);
        }

        var json = JsonConvert.DeserializeObject<dynamic>(propertyInfo.Top10Predictions);
        double totalSalePrice = 0;
        var count = 0;

        foreach (var prediction in json!)
        {
          totalSalePrice += (double)prediction.predictedSalePrice;
          count++;
        }

        var averageSalePrice = totalSalePrice / count; 

        marketDict[propertyInfo.Property.Country + " - " + propertyInfo.Property.Market].Add((propertyInfo.Property, averageSalePrice));
      }

      foreach (var (marketName, value) in marketDict)
      {
        var fileName = $"{marketName.Replace(" - ", "_")}_{DateTime.UtcNow:yyyyMMdd}.csv";
        await context.SaveLogEntry("CreatePropertyPredictionFile", $"Processing {fileName}", "Information", executionInstanceId);

        using var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, leaveOpen: true);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteHeader<Property>();
        csv.WriteField("AverageSalePrice"); 

        await csv.NextRecordAsync();

        foreach (var (property, avgPrice) in value)
        {
          csv.WriteRecord(property);
          csv.WriteField(avgPrice);
          await csv.NextRecordAsync();
        }
          
        memoryStream.Seek(0, SeekOrigin.Begin);
        await UploadFileToBlobStorage(memoryStream, fileName);

        await context.SaveLogEntry("CreatePropertyPredictionFile", $"Completing processing {fileName}", "Information", executionInstanceId);
      }

      if (myTimer.ScheduleStatus is not null)
      {
        _logger.LogInformation(
          $"Next timer for CreatePropertyPredictionFile is schedule at: {myTimer.ScheduleStatus.Next}");
      }

      await context.SaveLogEntry("CreatePropertyPredictionFile", "Completed", "Information", executionInstanceId);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("CreatePropertyPredictionFile", ex, executionInstanceId);
    }
  }

  private async Task UploadFileToBlobStorage(MemoryStream memoryStream, string fileName)
  {
    var managedIdentityId = config["ManagedIdentityId"]!;
    var cred = new ManagedIdentityCredential(managedIdentityId);

    var blobServiceEndpoint = $"https://{config["StorageAccountName"]}.blob.core.windows.net";
    var blobServiceClient = new BlobServiceClient(new Uri(blobServiceEndpoint), cred);
    var blobContainerClient = blobServiceClient.GetBlobContainerClient(config["StorageAccountContainer"]);

    var blobClient = blobContainerClient.GetBlobClient(fileName);
    await blobClient.UploadAsync(memoryStream, overwrite: true);
  }
}