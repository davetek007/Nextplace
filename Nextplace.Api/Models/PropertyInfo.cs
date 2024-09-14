using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PropertyInfo(
    long id,
    double longitude,
    double latitude,
    int daysOnMarket,
    double listingPrice)
{
    [Required]
    public long Id { get; } = id;

    [Required]
    public double Longitude { get; set; } = longitude;

    [Required]
    public double Latitude { get; set; } = latitude;

    [Required]
    public int DaysOnMarket { get; } = daysOnMarket;

    [Required]
    public double ListingPrice { get; set; } = listingPrice;
}