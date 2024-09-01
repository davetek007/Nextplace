using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PostPredictionRequest(double propertyLongitude, double propertyLatitude, string minerHotKey, string minerColdKey, DateTime predictionDate, float predictedSalePrice, DateTime predictedSaleDate)
{
    [Required]
    public double PropertyLongitude { get; } = propertyLongitude;

    [Required]
    public double PropertyLatitude { get; } = propertyLatitude;

    [Required]
    public string MinerHotKey { get; } = minerHotKey;

    [Required]
    public string MinerColdKey { get; } = minerColdKey;

    [Required]
    public DateTime PredictionDate { get; } = predictionDate;

    [Required]
    public float PredictedSalePrice { get; } = predictedSalePrice;

    [Required]
    public DateTime PredictedSaleDate { get; } = predictedSaleDate;
}