using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PostMinerScoreRequest(string minerHotKey, string minerColdKey, float minerScore, int numPredictions, DateTime scoreGenerationDate)
{
    [Required]
    public string MinerHotKey { get; } = minerHotKey;

    [Required]
    public string MinerColdKey { get; } = minerColdKey;

    [Required]
    public float MinerScore { get; } = minerScore;

    [Required]
    public int NumPredictions { get; } = numPredictions;

    [Required]
    public DateTime ScoreGenerationDate { get; } = scoreGenerationDate;
}