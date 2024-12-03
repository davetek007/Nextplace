using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nextplace.Functions.Db;
using Nextplace.Functions.Helpers;

namespace Nextplace.Functions.Functions;

public sealed class SyncMinerStatus(ILoggerFactory loggerFactory, IConfiguration configuration, AppDbContext context)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SyncMinerStatus>();

    [Function("SyncMinerStatus")]
    public async Task Run([TimerTrigger("%SyncMinerStatusTimerSchedule%")] TimerInfo timer)
    {
        var executionInstanceId = Guid.NewGuid().ToString();
        try
        {
            _logger.LogInformation($"SyncMinerStatus executed at: {DateTime.Now}");
            await context.SaveLogEntry("SyncMinerStatus", "Started", "Information", executionInstanceId);

            var minersRegisteredOnSubnet = await new ChainApiHelper(configuration).GetMiners();
            await context.SaveLogEntry("SyncMinerStatus", $"{minersRegisteredOnSubnet.Count} miners registered on the subnet", "Information", executionInstanceId);

            if (minersRegisteredOnSubnet.Count == 0)
            {
                await context.SaveLogEntry("SyncMinerStatus", "Failed to get miners registered on the subnet - exiting", "Error", executionInstanceId);
                _logger.LogError("Failed to get miners registered on subnet");
                return;
            }

            var minersInDb = await context.Miner.ToListAsync();
            var inserts = 0;
            var updates = 0;
            var deletes = 0;

            foreach (var miner in minersInDb)
            {
                var minerDetails = minersRegisteredOnSubnet.FirstOrDefault(k => k.Hotkey.Ss58 == miner.HotKey && k.Coldkey.Ss58 == miner.ColdKey);

                if (minerDetails == null)
                {
                    if (!miner.Active) continue;

                    miner.LastUpdateDate = DateTime.UtcNow;
                    miner.Active = false;

                    deletes++;
                }
                else
                {
                    miner.LastUpdateDate = DateTime.UtcNow;
                    miner.Active = true;
                    miner.Incentive = minerDetails.Incentive;
                    miner.Uid = minerDetails.Uid;

                    updates++;
                }
            }

            foreach (var item in minersRegisteredOnSubnet)
            {
                if (minersInDb.Any(m => m.HotKey == item.Hotkey.Ss58 && m.ColdKey == item.Coldkey.Ss58))
                {
                    continue;
                }

                var miner = new Miner
                {
                    HotKey = item.Hotkey.Ss58,
                    ColdKey = item.Coldkey.Ss58,
                    Incentive = item.Incentive,
                    Uid = item.Uid,
                    CreateDate = DateTime.UtcNow,
                    LastUpdateDate = DateTime.UtcNow,
                    Active = true
                };

                context.Miner.Add(miner);
                inserts++;
            }

            await context.SaveLogEntry("SyncMinerStatus", $"{inserts} Inserts, {updates} Updates, {deletes} Deletes", "Information", executionInstanceId);
            await context.SaveLogEntry("SyncMinerStatus", "Saving to DB", "Information", executionInstanceId);
            await context.SaveChangesAsync();

            if (timer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next SyncMinerStatus schedule at: {timer.ScheduleStatus.Next}");
            }
            await context.SaveLogEntry("SyncMinerStatus", "Completed", "Information", executionInstanceId);
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("SyncMinerStatus", ex, executionInstanceId);
        }
    }
}