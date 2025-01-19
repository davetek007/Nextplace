using System.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nextplace.Functions.Db;

namespace Nextplace.Functions.Functions;

public sealed class DeDuplicatePropertyPredictions(ILoggerFactory loggerFactory, AppDbContext context)
{
  private readonly ILogger _logger = loggerFactory.CreateLogger<DeDuplicatePropertyPredictions>();

  [Function("DeDuplicatePropertyPredictions")]
  public async Task Run([TimerTrigger("%DeDuplicatePropertyPredictionsTimerSchedule%")] TimerInfo myTimer)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      _logger.LogInformation($"DeDuplicatePropertyPredictions executed at: {DateTime.UtcNow}");
      await context.SaveLogEntry("DeDuplicatePropertyPredictions", "Started", "Information", executionInstanceId);

      await using var connection = context.Database.GetDbConnection() as SqlConnection;
      await using var command = new SqlCommand("dbo.DeDuplicatePropertyPredictions", connection);

      command.CommandType = CommandType.StoredProcedure;
      command.CommandTimeout = 600;

      command.Parameters.Add(new SqlParameter("@executionInstanceId", executionInstanceId));

      await connection?.OpenAsync()!;
      await command.ExecuteNonQueryAsync();

      if (myTimer.ScheduleStatus is not null)
      {
        _logger.LogInformation(
          $"Next timer for DeDuplicatePropertyPredictions is schedule at: {myTimer.ScheduleStatus.Next}");
      }

      await context.SaveLogEntry("DeDuplicatePropertyPredictions", "Completed", "Information", executionInstanceId);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("DeDuplicatePropertyPredictions", ex, executionInstanceId);
    }
  }
}