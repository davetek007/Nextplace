using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class MinerScore(double score, int numPredictions, int totalPredictions, DateTime scoreGenerationDate, Validator? validator)
{
    [Required] public double Score { get; } = score;

    [Required] public int NumPredictions { get; } = numPredictions;

    [Required] public int TotalPredictions { get; } = totalPredictions;

    [Required] public DateTime ScoreGenerationDate { get; } = scoreGenerationDate;

    public Validator? Validator { get; } = validator;
}