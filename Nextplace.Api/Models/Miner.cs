using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class Miner(string hotKey, string coldKey, DateTime? firstSeenOnSubnetDate, DateTime? lastSeenOnSubnetDate, bool activeOnSubnet, double incentive, int uid)
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
    public int Uid { get; } = uid;

    [Required]
    public double Incentive { get; } = incentive;

    public double? MinScore { get; init; }

    public double? AvgScore { get; init; }

    public double? MaxScore { get; init; }

    public int? NumScores { get; init; }

    public int? NumPredictions { get; init; }

    public DateTime? ScoreGenerationDate { get; init; }
}