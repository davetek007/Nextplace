using Microsoft.AspNetCore.Datasync.EFCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Db;

public sealed class PropertyPrediction : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    public required long PropertyId { get; init; }

    public required long MinerId { get; init; }

    public long ValidatorId { get; set; }

    public required DateTime PredictionDate { get; set; }

    public required DateTime PredictedSaleDate { get; init; }

    public double? PredictionScore { get; set; }

    public required double PredictedSalePrice { get; init; }

    public required DateTime CreateDate { get; set; }

    public required DateTime LastUpdateDate { get; set; }

    public required bool Active { get; set; }

    public Miner Miner { get; init; } = null!;

    public Validator Validator { get; init; } = null!;

    public Property Property { get; init; } = null!;
}