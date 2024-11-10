using System.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nextplace.Functions.Db;

namespace Nextplace.Functions.Functions;

public sealed class CalculatePropertyEstimateStats(ILoggerFactory loggerFactory, AppDbContext context)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<CalculatePropertyEstimateStats>(); 

    [Function("CalculatePropertyEstimateStats")]
    public async Task Run([TimerTrigger("%CalculatePropertyEstimateStatsTimerSchedule%")] TimerInfo myTimer)
    { 
        var executionInstanceId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation($"CalculatePropertyEstimateStats executed at: {DateTime.UtcNow}");
            await context.SaveLogEntry("CalculatePropertyEstimateStats", "Started", "Information", executionInstanceId);

            await using var connection = context.Database.GetDbConnection() as SqlConnection;
            await using var command = new SqlCommand("dbo.CalculatePropertyEstimateStats", connection);
            
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 600;

            await connection?.OpenAsync()!;
            await command.ExecuteNonQueryAsync();
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation(
                    $"Next timer for CalculatePropertyEstimateStats is schedule at: {myTimer.ScheduleStatus.Next}");
            }
            
            await context.SaveLogEntry("CalculatePropertyEstimateStats", "Completed", "Information", executionInstanceId);
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("CalculatePropertyEstimateStats", ex, executionInstanceId);
        }
    }
}