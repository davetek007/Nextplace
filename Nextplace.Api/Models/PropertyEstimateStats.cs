using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PropertyEstimateStats(DateTime firstEstimateDate, DateTime lastEstimateDate, int numEstimates,
    double avgEstimate, double minEstimate, double maxEstimate, double closestEstimate, double firstEstimateAmount, double lastEstimateAmount)
{
    [Required]
    public DateTime FirstEstimateDate { get; } = firstEstimateDate;
    
    [Required]
    public DateTime LastEstimateDate { get; } = lastEstimateDate;

    [Required]
    public int NumEstimates { get; } = numEstimates;

    [Required]
    public double AvgEstimate { get; } = avgEstimate;

    [Required]
    public double MinEstimate { get; } = minEstimate;

    [Required]
    public double MaxEstimate { get; } = maxEstimate;

    [Required]
    public double ClosestEstimate { get; } = closestEstimate;

    [Required]
    public double FirstEstimateAmount { get; } = firstEstimateAmount;

    [Required]
    public double LastEstimateAmount { get; } = lastEstimateAmount;
}