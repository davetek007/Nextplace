using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PropertyPredictionStats(int numPredictions, double avgPredictedSalePrice, double minPredictedSalePrice, double maxPredictedSalePrice)
{
  [Required]
  public int NumPredictions { get; } = numPredictions;

  [Required]
  public double AvgPredictedSalePrice { get; } = avgPredictedSalePrice;

  [Required]
  public double MinPredictedSalePrice { get; } = minPredictedSalePrice;

  [Required]
  public double MaxPredictedSalePrice { get; } = maxPredictedSalePrice;
}