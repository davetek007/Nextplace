using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class TrendingMarket(string marketName, int numberOfPredictions)
{
    [Required]
    public string MarketName { get; } = marketName;

    [Required]
    public int NumberOfPredictions { get; } = numberOfPredictions;
}