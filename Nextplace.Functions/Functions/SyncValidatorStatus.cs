using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nextplace.Functions.Db;
using Nextplace.Functions.Helpers;

namespace Nextplace.Functions.Functions;

public sealed class SyncValidatorStatus(ILoggerFactory loggerFactory, IConfiguration configuration, AppDbContext context)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SyncValidatorStatus>();

    [Function("SyncValidatorStatus")]
    public async Task Run([TimerTrigger("%SyncValidatorStatusTimerSchedule%")] TimerInfo timer)
    {
        var executionInstanceId = Guid.NewGuid().ToString();
        try
        {
            _logger.LogInformation($"SyncValidatorStatus executed at: {DateTime.Now}");
            await context.SaveLogEntry("SyncValidatorStatus", "Started", "Information", executionInstanceId);

            var validatorsRegisteredOnSubnet = await new ChainApiHelper(configuration).GetValidators();
            await context.SaveLogEntry("SyncValidatorStatus", $"{validatorsRegisteredOnSubnet.Count} validators registered on the subnet", "Information", executionInstanceId);

            if (validatorsRegisteredOnSubnet.Count == 0)
            {
                await context.SaveLogEntry("SyncValidatorStatus", "Failed to get validators registered on the subnet - exiting", "Error", executionInstanceId);
                _logger.LogError("Failed to get validators registered on subnet");
                return;
            }

            var validatorsInDb = await context.Validator.ToListAsync();
            var inserts = 0;
            var updates = 0;
            var deletes = 0;

            foreach (var validator in validatorsInDb)
            {
                var validatorDetails = validatorsRegisteredOnSubnet.FirstOrDefault(v => v.Coldkey.Ss58 == validator.ColdKey && v.Hotkey.Ss58 == validator.HotKey);

                if (validatorDetails == null)
                {
                    if (!validator.Active) continue;

                    validator.LastUpdateDate = DateTime.UtcNow;
                    validator.Active = false;

                    deletes++;
                }
                else
                {
                    validator.LastUpdateDate = DateTime.UtcNow;
                    validator.IpAddress = validatorDetails.AxonInfo!.Ip;
                    validator.Incentive = validatorDetails.Incentive;
                    validator.Active = true;

                    updates++;
                }
            }

            foreach (var validatorDetails in validatorsRegisteredOnSubnet)
            {
                if (validatorsInDb.Any(v => v.HotKey == validatorDetails.Hotkey.Ss58 && v.ColdKey == validatorDetails.Coldkey.Ss58))
                {
                    continue;
                }

                var validator = new Validator
                {
                    HotKey = validatorDetails.Hotkey.Ss58,
                    ColdKey = validatorDetails.Coldkey.Ss58,
                    IpAddress = validatorDetails.AxonInfo!.Ip,
                    Incentive = validatorDetails.Incentive,
                    CreateDate = DateTime.UtcNow,
                    LastUpdateDate = DateTime.UtcNow,
                    Active = true
                };

                context.Validator.Add(validator);
                inserts++;
            }

            await context.SaveLogEntry("SyncValidatorStatus", $"{inserts} Inserts, {updates} Updates, {deletes} Deletes", "Information", executionInstanceId);
            await context.SaveLogEntry("SyncValidatorStatus", "Saving to DB", "Information", executionInstanceId);
            await context.SaveChangesAsync();

            if (timer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next SyncValidatorStatus schedule at: {timer.ScheduleStatus.Next}");
            }
            await context.SaveLogEntry("SyncValidatorStatus", "Completed", "Information", executionInstanceId);
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("SyncValidatorStatus", ex, executionInstanceId);
        }
    }
}