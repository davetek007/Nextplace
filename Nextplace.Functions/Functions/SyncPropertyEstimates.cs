using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nextplace.Functions.Db;
using Nextplace.Functions.Helpers;
using Newtonsoft.Json;
using Nextplace.Functions.Models.SyncPropertyEstimates;

namespace Nextplace.Functions.Functions;

public sealed class SyncPropertyEstimates(ILoggerFactory loggerFactory, IConfiguration configuration, AppDbContext context)
{
  private readonly ILogger _logger = loggerFactory.CreateLogger<SyncPropertyEstimates>();
  private string? _apiKey;

  [Function("SyncPropertyEstimates")]
  public async Task Run([TimerTrigger("%SyncPropertyEstimatesTimerSchedule%")] TimerInfo myTimer)
  {
    const string country = @"United States";
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      _logger.LogInformation($"SyncPropertyEstimates executed at: {DateTime.UtcNow}");
      await context.SaveLogEntry("SyncPropertyEstimates", "Started", "Information", executionInstanceId);

      _apiKey = await new AkvHelper(configuration).GetSecretAsync("ZillowApiKey");

      var properties = context.Property
          .Where(p => p.Active && !p.Estimates!.Any() && p.SaleDate != null && p.Address != null &&
                      p.Country == country && p.City != null && p.State != null && p.ZipCode != null && !p.EstimatesCollected)
          .ToList();

      await context.SaveLogEntry("SyncPropertyEstimates", $"Processing {properties.Count} sold properties", "Information", executionInstanceId);

      foreach (var property in properties)
      {
        await GetEstimate(property, executionInstanceId);

        property.EstimatesCollected = true;
        property.LastUpdateDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
      }

      if (myTimer.ScheduleStatus is not null)
      {
        _logger.LogInformation(
            $"Next timer for SyncProperties is schedule at: {myTimer.ScheduleStatus.Next}");
      }
      await context.SaveLogEntry("SyncPropertyEstimates", "Completed", "Information", executionInstanceId);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("SyncPropertyEstimates", ex, executionInstanceId);
    }
  }

  private async Task GetEstimate(Property property, string executionInstanceId)
  {
    try
    {
      await context.SaveLogEntry("SyncPropertyEstimates", $"Getting estimate for property ID {property.Id}", "Information", executionInstanceId);

      var searchString = property.Address + " " + property.City + " " + property.State + " " + property.ZipCode;
      searchString = searchString.Replace(" ", "-");

      const int maxRequestsPerSecond = 3;
      var estimates = await GetPropertyEstimates(searchString, maxRequestsPerSecond);

      await context.SaveLogEntry("SyncPropertyEstimates", $"{estimates?.Count ?? 0} estimates obtained for property ID {property.Id}", "Information", executionInstanceId);

      if (estimates != null)
      {
        foreach (var estimate in estimates)
        {
          var timeStamp = FromUnixTimeStamp(estimate.Timestamp);

          var propertyEstimate = new PropertyEstimate
          {
            PropertyId = property.Id,
            Estimate = estimate.Value,
            DateEstimated = timeStamp,
            CreateDate = DateTime.UtcNow,
            LastUpdateDate = DateTime.UtcNow,
            Active = true
          };

          context.PropertyEstimate.Add(propertyEstimate);
        }

        await context.SaveChangesAsync();
      }
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("SyncPropertyEstimates", new Exception($"Error obtaining estimate for property {property.Id}", ex), executionInstanceId);
    }
  }

  private async Task<List<Estimate>?> GetPropertyEstimates(string searchString, int maxRequestsPerSecond)
  {
    string? responseBody = null;
    var url = configuration["ZillowApiUrl"];
    using var httpClient = new HttpClient();

    httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", configuration["ZillowApiHost"]);
    httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", _apiKey);

    var delayBetweenRequests = 1000 / maxRequestsPerSecond;

    try
    {
      var retryCount = 0;
      const int maxRetries = 3;
      bool shouldRetry;

      do
      {
        shouldRetry = false;
        var response = await httpClient.GetAsync(url + searchString);

        if (response.StatusCode == (System.Net.HttpStatusCode)429)
        {
          retryCount++;

          if (response.Headers.TryGetValues("Retry-After", out var retryAfterValues))
          {
            if (int.TryParse(retryAfterValues.FirstOrDefault(), out var retryAfterSeconds))
            {
              await Task.Delay(retryAfterSeconds * 1000);
            }
          }
          else
          {
            await Task.Delay(1000 * retryCount);
          }

          shouldRetry = retryCount < maxRetries;
        }
        else
        {
          response.EnsureSuccessStatusCode();
          responseBody = await response.Content.ReadAsStringAsync();

          await Task.Delay(delayBetweenRequests);
        }
      }
      while (shouldRetry);

      if (responseBody is null or "{}")
      {
        return null;
      }

      var rootObject = JsonConvert.DeserializeObject<List<Estimate>>(responseBody);
      return rootObject;
    }
    catch (Exception ex)
    {
      if (!string.IsNullOrWhiteSpace(responseBody))
      {
        throw new Exception($"Error received from API call: {responseBody}", ex);
      }

      throw;
    }
  }

  private static DateTime FromUnixTimeStamp(long timestamp)
  {
    var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timestamp);
    return dateTimeOffset.UtcDateTime;
  }
}