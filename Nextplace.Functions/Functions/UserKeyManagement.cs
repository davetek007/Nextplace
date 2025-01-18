using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nextplace.Functions.Db;

namespace Nextplace.Functions.Functions;

public sealed class UserKeyManagement(ILoggerFactory loggerFactory, IConfiguration configuration, AppDbContext context)
{
  private readonly ILogger _logger = loggerFactory.CreateLogger<UserKeyManagement>();

  [Function("UserKeyManagement")]
  public async Task Run([TimerTrigger("%UserKeyManagementTimerSchedule%")]
    TimerInfo timer)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      _logger.LogInformation($"UserKeyManagement executed at: {DateTime.Now}");
      await context.SaveLogEntry("UserKeyManagement", "Started", "Information", executionInstanceId);

      var userKeyInvalidationPeriodMinutes = Convert.ToInt32(configuration["UserKeyInvalidationPeriodMinutes"]);
      var lastUpdateThreshold = DateTime.UtcNow.AddMinutes(0 - userKeyInvalidationPeriodMinutes);

      var users = await context.User.Where(m => m.Active && m.LastUpdateDate < lastUpdateThreshold).ToListAsync();

      await context.SaveLogEntry("UserKeyManagement", $"Processing {users.Count} users", "Information", executionInstanceId);

      foreach (var user in users)
      {
        user.SessionToken = null;
        user.ValidationKey = null;
        user.LastUpdateDate = DateTime.UtcNow;
      }

      await context.SaveLogEntry("UserKeyManagement", "Saving to DB", "Information", executionInstanceId);

      await context.SaveChangesAsync();

      if (timer.ScheduleStatus is not null)
      {
        _logger.LogInformation($"Next UserKeyManagement schedule at: {timer.ScheduleStatus.Next}");
      }

      await context.SaveLogEntry("UserKeyManagement", "Completed", "Information", executionInstanceId);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("UserKeyManagement", ex, executionInstanceId);
    }
  }
}