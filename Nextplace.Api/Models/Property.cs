using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class Property(
    long id,
    long propertyId,
    string nextplaceId,
    long listingId,
    double longitude,
    double latitude,
    string market,
    string? city,
    string? state,
    string? zipCode,
    string? address,
    DateTime listingDate,
    double listingPrice,
    int? numberOfBeds,
    double? numberOfBaths,
    int? squareFeet,
    long? lotSize,
    int? yearBuilt,
    string propertyType,
    DateTime? lastSaleDate,
    int? hoaDues,
    DateTime? saleDate,
    double? salePrice,
    DateTime createDate,
    DateTime lastUpdateDate,
    bool active, string? country)
{
  [Required]
  public long Id { get; } = id;

  [Required]
  [MaxLength(450)]
  public long PropertyId { get; } = propertyId;

  [Required]
  [MaxLength(450)]
  public string NextplaceId { get; } = nextplaceId;

  [Required]
  [MaxLength(450)]
  public long ListingId { get; } = listingId;

  [Required]
  public double Longitude { get; set; } = longitude;

  [Required]
  public double Latitude { get; set; } = latitude;

  [Required]
  public string Market { get; } = market;

  public string? City { get; } = city;

  public string? State { get; } = state;

  public string? ZipCode { get; } = zipCode;

  public string? Country { get; } = country;

  public string? Address { get; } = address;

  [Required]
  public DateTime ListingDate { get; } = listingDate;

  [Required]
  public double ListingPrice { get; set; } = listingPrice;

  public int? NumberOfBeds { get; } = numberOfBeds;

  public double? NumberOfBaths { get; } = numberOfBaths;

  public int? SquareFeet { get; } = squareFeet;

  public long? LotSize { get; } = lotSize;

  public int? YearBuilt { get; } = yearBuilt;

  [Required]
  [MaxLength(450)]
  public string PropertyType { get; } = propertyType;

  public DateTime? LastSaleDate { get; } = lastSaleDate;

  public int? HoaDues { get; } = hoaDues;

  public DateTime? SaleDate { get; } = saleDate;

  public double? SalePrice { get; set; } = salePrice;

  [Required]
  public DateTime CreateDate { get; } = createDate;

  [Required]
  public DateTime LastUpdateDate { get; } = lastUpdateDate;

  [Required]
  public bool Active { get; } = active;

  [Required]
  public List<PropertyPrediction> Predictions { get; set; } = null!;

  public PropertyEstimateStats? EstimateStats { get; set; } = null!;
}