using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class Miner(string hotKey, string coldKey, DateTime? firstSeenOnSubnetDate, DateTime? lastSeenOnSubnetDate, bool activeOnSubnet, double incentive)
{
    [Required]
    public string HotKey { get; } = hotKey;

    [Required]
    public string ColdKey { get; } = coldKey;

    [Required]
    public DateTime? FirstSeenOnSubnetDate { get; } = firstSeenOnSubnetDate;

    [Required]
    public DateTime? LastSeenOnSubnetDate { get; } = lastSeenOnSubnetDate;

    [Required]
    public bool ActiveOnSubnet { get; } = activeOnSubnet;

    [Required]
    public double Incentive{ get; } = incentive;
}