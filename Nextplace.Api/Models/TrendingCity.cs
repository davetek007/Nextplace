using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class TrendingCity(string cityName, int numberOfPredictions)
{
    [Required]
    public string CityName { get; } = cityName;

    [Required]
    public int NumberOfPredictions { get; } = numberOfPredictions;
}