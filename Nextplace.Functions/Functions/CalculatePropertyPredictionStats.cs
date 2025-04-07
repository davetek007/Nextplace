using System.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nextplace.Functions.Db;

namespace Nextplace.Functions.Functions;

public sealed class CalculatePropertyPredictionStats(ILoggerFactory loggerFactory, AppDbContext context)
{
  private readonly ILogger _logger = loggerFactory.CreateLogger<CalculatePropertyPredictionStats>();

  [Function("CalculatePropertyPredictionStats")]
  public async Task Run([TimerTrigger("%CalculatePropertyPredictionStatsTimerSchedule%")] TimerInfo myTimer)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      _logger.LogInformation($"CalculatePropertyPredictionStats executed at: {DateTime.UtcNow}");
      await context.SaveLogEntry("CalculatePropertyPredictionStats", "Started", "Information", executionInstanceId);

      await using var connection = context.Database.GetDbConnection() as SqlConnection;
      await using var command = new SqlCommand("dbo.CalculatePropertyPredictionStats", connection);

      command.CommandType = CommandType.StoredProcedure;
      command.CommandTimeout = 18000;

      command.Parameters.Add(new SqlParameter("@executionInstanceId", executionInstanceId));

      await connection?.OpenAsync()!;
      await command.ExecuteNonQueryAsync();

      if (myTimer.ScheduleStatus is not null)
      {
        _logger.LogInformation(
            $"Next timer for CalculatePropertyPredictionStats is schedule at: {myTimer.ScheduleStatus.Next}");
      }

      await context.SaveLogEntry("CalculatePropertyPredictionStats", "Completed", "Information", executionInstanceId);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("CalculatePropertyPredictionStats", ex, executionInstanceId);
    }
  }
}