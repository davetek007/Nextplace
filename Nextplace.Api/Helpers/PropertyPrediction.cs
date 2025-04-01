namespace Nextplace.Api.Helpers;

public class PropertyPredictionInfo
{
  public required long PropertyId { get; init; }

  public required long MinerId { get; init; }

  public long? ValidatorId { get; set; }

  public required DateTime PredictionDate { get; set; }

  public required DateTime PredictedSaleDate { get; init; }

  public double? PredictionScore { get; set; }

  public required double PredictedSalePrice { get; init; }

  public required DateTime CreateDate { get; set; }
}