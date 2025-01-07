using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class MinerDatedScore(DateTime date, int totalScored)
{

  [Required]
  public DateTime Date { get; } = date;

  [Required]
  public int TotalScored { get; } = totalScored;
}