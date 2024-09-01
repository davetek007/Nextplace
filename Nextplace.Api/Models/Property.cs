using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class Property(long id, double longitude, double latitude, string market, string city, string state, string zipCode, string address, DateTime listingDate, double listingPrice, DateTime? saleDate, double? salePrice)
{
    [Required]
    public long Id { get; } = id;

    [Required]
    public double Longitude { get; set; } = longitude;

    [Required]
    public double Latitude { get; set; } = latitude;

    [Required]
    public string Market { get; } = market;

    [Required]
    public string City { get; } = city;

    [Required]
    public string State { get; } = state;

    [Required]
    public string ZipCode { get; } = zipCode;

    [Required]
    public string Address { get; } = address;

    [Required]
    public DateTime ListingDate { get; } = listingDate;

    [Required]
    public double ListingPrice { get; set; } = listingPrice;

    public DateTime? SaleDate { get; } = saleDate;

    public double? SalePrice { get; set; } = salePrice;

    [Required]
    public List<PropertyPrediction> Predictions { get; set; } = null!;
}