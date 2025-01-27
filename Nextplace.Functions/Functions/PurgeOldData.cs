using System.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nextplace.Functions.Db;

namespace Nextplace.Functions.Functions;

public sealed class PurgeOldData(ILoggerFactory loggerFactory, AppDbContext context)
{
  private readonly ILogger _logger = loggerFactory.CreateLogger<PurgeOldData>();

  [Function("PurgeOldData")]
  public async Task Run([TimerTrigger("%PurgeOldDataTimerSchedule%")] TimerInfo myTimer)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      _logger.LogInformation($"PurgeOldData executed at: {DateTime.UtcNow}");
      await context.SaveLogEntry("PurgeOldData", "Started", "Information", executionInstanceId);

      await using var connection = context.Database.GetDbConnection() as SqlConnection;
      await connection?.OpenAsync()!;
      await using var command1 = new SqlCommand("dbo.DeleteOldProperties", connection);

      command1.CommandType = CommandType.StoredProcedure;
      command1.CommandTimeout = 1800;

      command1.Parameters.Add(new SqlParameter("@executionInstanceId", executionInstanceId));
      
      await command1.ExecuteNonQueryAsync();
      
      
      await using var command2 = new SqlCommand("dbo.DeleteOldMinerStats", connection);

      command2.CommandType = CommandType.StoredProcedure;
      command2.CommandTimeout = 1800;

      command2.Parameters.Add(new SqlParameter("@executionInstanceId", executionInstanceId));
      
      await command2.ExecuteNonQueryAsync();
      
      
      await connection.CloseAsync();

      if (myTimer.ScheduleStatus is not null)
      {
        _logger.LogInformation(
            $"Next timer for PurgeOldData is schedule at: {myTimer.ScheduleStatus.Next}");
      }

      await context.SaveLogEntry("PurgeOldData", "Completed", "Information", executionInstanceId);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("PurgeOldData", ex, executionInstanceId);
    }
  }
}