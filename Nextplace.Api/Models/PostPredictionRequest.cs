using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PostPredictionRequest(string nextplaceId, string minerHotKey, string minerColdKey, float? predictionScore, DateTime predictionDate, float predictedSalePrice, DateTime predictedSaleDate)
{
    [Required]
    public string NextplaceId { get; } = nextplaceId;

    [Required]
    public string MinerHotKey { get; } = minerHotKey;

    [Required]
    public string MinerColdKey { get; } = minerColdKey;

    public float? PredictionScore { get; } = predictionScore;

    [Required]
    public DateTime PredictionDate { get; } = predictionDate;

    [Required]
    public float PredictedSalePrice { get; } = predictedSalePrice;

    [Required]
    public DateTime PredictedSaleDate { get; } = predictedSaleDate;
}