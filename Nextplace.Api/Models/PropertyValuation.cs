using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PropertyValuation(
    long id,
    string nextplaceId,
    double longitude,
    double latitude,
    string? city,
    string? state,
    string? zipCode,
    string? address,
    int? numberOfBeds,
    double? numberOfBaths,
    int? squareFeet,
    long? lotSize,
    int? yearBuilt,
    int? hoaDues,
    int propertyType,
    double proposedListingPrice,
    DateTime createDate,
    DateTime lastUpdateDate,
    bool active,
    string? country,
    double? listingPrice)
{
  [Required]
  public long Id { get; } = id;

  [Required]
  [MaxLength(450)]
  public string NextplaceId { get; } = nextplaceId;

  [Required]
  public double Longitude { get; set; } = longitude;

  [Required]
  public double Latitude { get; set; } = latitude;
  [Required]
  public int PropertyType { get; } = propertyType;

  [Required]
  public double ProposedListingPrice { get; set; } = proposedListingPrice;

  [Required]
  public double? ListingPrice { get; set; } = listingPrice;

  public string? City { get; } = city;

  public string? State { get; } = state;

  public string? ZipCode { get; } = zipCode;

  public string? Address { get; } = address;

  public string? Country { get; } = country;

  public int? NumberOfBeds { get; } = numberOfBeds;

  public double? NumberOfBaths { get; } = numberOfBaths;

  public int? SquareFeet { get; } = squareFeet;

  public long? LotSize { get; } = lotSize;

  public int? YearBuilt { get; } = yearBuilt;

  public int? HoaDues { get; } = hoaDues;

  [Required]
  public DateTime CreateDate { get; } = createDate;

  [Required]
  public DateTime LastUpdateDate { get; } = lastUpdateDate;

  [Required]
  public bool Active { get; } = active;
}