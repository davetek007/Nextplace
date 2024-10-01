using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PropertyEstimate(DateTime estimateDate, double estimatedAmount)
{
    [Required]
    public DateTime EstimateDate { get; } = estimateDate;

    [Required]
    public double EstimatedAmount { get; } = estimatedAmount;
}