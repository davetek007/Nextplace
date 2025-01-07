using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Api.Db;

public sealed class MinerDatedScore : EntityTableData
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public new long Id { get; init; }

  public required long MinerScoreId { get; init; }
   
  public required int TotalScored { get; set; }

  public required DateTime Date { get; set; }

  public required DateTime CreateDate { get; set; }

  public required DateTime LastUpdateDate { get; set; }

  public required bool Active { get; set; }

  public MinerScore MinerScore { get; init; } = null!; 
}