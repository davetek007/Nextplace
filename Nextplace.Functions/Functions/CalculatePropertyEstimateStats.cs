using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Nextplace.Functions.Db;
using Microsoft.EntityFrameworkCore;

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

            context.Database.SetCommandTimeout(2000);

            const int batchSize = 1000;
            var totalProperties = context.Property
                .Include(p => p.Estimates)
                .Include(p => p.EstimateStats)
                .Count(p => p.Active && p.Estimates!.Any() && p.SaleDate == null &&
                            p.EstimateStats!.Count(e => e.Active && e.CreateDate > DateTime.UtcNow.AddHours(-1)) == 0);

            await context.SaveLogEntry("CalculatePropertyEstimateStats", $"Total properties to process: {totalProperties}", "Information", executionInstanceId);


            for (var i = 0; i < totalProperties; i += batchSize)
            {
                var propertiesBatch = context.Property
                    .Include(p => p.Estimates)
                    .Include(p => p.EstimateStats)
                    .Where(p => p.Active && p.Estimates!.Any() && p.SaleDate == null &&
                                p.EstimateStats!.Count(e => e.Active && e.CreateDate > DateTime.UtcNow.AddHours(-1)) == 0)
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                await context.SaveLogEntry("CalculatePropertyEstimateStats", $"Calculating Stats for batch {i / batchSize + 1} of {totalProperties / batchSize + 1}", "Information", executionInstanceId);

                foreach (var property in propertiesBatch)
                {
                    await CalculateStats(property);
                }

                await context.SaveChangesAsync();
            }

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

    private async Task CalculateStats(Property property)
    {
        var propertySaleDate = property.SaleDate ?? DateTime.MaxValue;
        var propertySalePrice = property.SalePrice ?? 0;

        var estimates = property.Estimates!.Where(p => p.DateEstimated < propertySaleDate).ToList();

        if (estimates.Count == 0)
        {
            return;
        }

        var firstEstimateDate = estimates.Select(p => p.DateEstimated).First();
        var lastEstimateDate = estimates.Select(p => p.DateEstimated).Last();
        var firstEstimateAmount =
            estimates.OrderBy(p => p.DateEstimated).Select(p => p.Estimate).First();
        var lastEstimateAmount = estimates.OrderByDescending(p => p.DateEstimated)
        .Select(p => p.Estimate)
            .First();
        var numEstimates = estimates.Count;
        var avgEstimate = estimates.Average(p => p.Estimate);
        var minEstimate = estimates.Min(p => p.Estimate);
        var maxEstimate = estimates.Max(p => p.Estimate);
        var closestEstimate = estimates.OrderBy(p => Math.Abs(p.Estimate - propertySalePrice))
            .Select(p => p.Estimate).First();

        var estimateStats = new PropertyEstimateStats
        {
            Active = true, 
            AvgEstimate = avgEstimate, 
            CreateDate = DateTime.UtcNow, 
            ClosestEstimate = closestEstimate,
            FirstEstimateAmount = firstEstimateAmount,
            FirstEstimateDate = firstEstimateDate,
            LastUpdateDate = DateTime.UtcNow, 
            LastEstimateAmount = lastEstimateAmount,
            LastEstimateDate = lastEstimateDate, 
            MaxEstimate = maxEstimate, 
            MinEstimate = minEstimate,
            NumEstimates = numEstimates, 
            PropertyId = property.Id
        };

        context.PropertyEstimateStats.Add(estimateStats);
    }
}