using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class MarketSample(
    string market,
    List<PropertyInfo> properties)
{
    [Required]
    public string Market { get; } = market;

    [Required]
    public List<PropertyInfo> Properties { get; } = properties;
}