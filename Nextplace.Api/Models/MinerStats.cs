using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class MinerStats(Miner miner, int ranking)
{
    [Required] public Miner Miner { get; } = miner;

    [Required] public int Ranking { get; } = ranking;
}