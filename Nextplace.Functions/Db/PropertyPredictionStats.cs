using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Functions.Db;

public sealed class PropertyPredictionStats : EntityTableData
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public new long Id { get; init; }

  public required long PropertyId { get; init; }

  public required int NumPredictions { get; init; }

  public required double AvgPredictedSalePrice { get; set; }

  public required double MinPredictedSalePrice { get; set; }

  public required double MaxPredictedSalePrice { get; set; }

  public required string Top10Predictions { get; set; }

  public required DateTime CreateDate { get; set; }

  public required DateTime LastUpdateDate { get; set; }

  public required bool Active { get; set; }

  public Property Property { get; init; } = null!;
}