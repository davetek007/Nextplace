using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class MinerStats(Miner miner, int ranking, string statType, int numberOfPredictions, int correctPredictions)
{
    [Required] public Miner Miner { get; } = miner;

    [Required] public int Ranking { get; } = ranking;

    [Required] public string StatType { get; } = statType;

    [Required] public int NumberOfPredictions { get; } = numberOfPredictions;

    [Required] public int CorrectPredictions { get; } = correctPredictions;
}