using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class MarketSample(
    string market,
    string? country,
    List<PropertyInfo> properties)
{
  [Required]
  public string Market { get; } = market;
  [Required]
  public string? Country { get; } = country;

  [Required]
  public List<PropertyInfo> Properties { get; } = properties;
}