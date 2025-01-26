using System.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nextplace.Functions.Db;

namespace Nextplace.Functions.Functions;

public sealed class DeleteOldProperties(ILoggerFactory loggerFactory, AppDbContext context)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<DeleteOldProperties>(); 

    [Function("DeleteOldProperties")]
    public async Task Run([TimerTrigger("%DeleteOldPropertiesTimerSchedule%")] TimerInfo myTimer)
    { 
        var executionInstanceId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation($"DeleteOldProperties executed at: {DateTime.UtcNow}");
            await context.SaveLogEntry("DeleteOldProperties", "Started", "Information", executionInstanceId);

            await using var connection = context.Database.GetDbConnection() as SqlConnection;
            await using var command = new SqlCommand("dbo.DeleteOldProperties", connection);
            
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 1800;
            
            command.Parameters.Add(new SqlParameter("@executionInstanceId", executionInstanceId));

            await connection?.OpenAsync()!;
            await command.ExecuteNonQueryAsync();
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation(
                    $"Next timer for DeleteOldProperties is schedule at: {myTimer.ScheduleStatus.Next}");
            }
            
            await context.SaveLogEntry("DeleteOldProperties", "Completed", "Information", executionInstanceId);
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("DeleteOldProperties", ex, executionInstanceId);
        }
    }
}