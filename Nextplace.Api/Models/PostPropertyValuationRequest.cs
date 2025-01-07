using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PostPropertyValuationRequest(
    string requestorEmailAddress,
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
    string? country)
{
  [Required]
  public string RequestorEmailAddress { get; } = requestorEmailAddress;

  [Required]
  public double Longitude { get; set; } = longitude;

  [Required]
  public double Latitude { get; set; } = latitude;

  public string? City { get; } = city;

  public string? Country { get; } = country;

  public string? State { get; } = state;

  public string? ZipCode { get; } = zipCode;

  public string? Address { get; } = address;

  public int? NumberOfBeds { get; } = numberOfBeds;

  public double? NumberOfBaths { get; } = numberOfBaths;

  public int? SquareFeet { get; } = squareFeet;

  public long? LotSize { get; } = lotSize;

  public int? YearBuilt { get; } = yearBuilt;

  public int? HoaDues { get; } = hoaDues;

  public int PropertyType { get; } = propertyType;

  public double ProposedListingPrice { get; } = proposedListingPrice;
}