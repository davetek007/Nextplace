using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Api.Db;

public sealed class PropertyValuationPrediction : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    public required long PropertyValuationId { get; init; }

    public required long MinerId { get; init; }

    public long? ValidatorId { get; set; }

    public required DateTime PredictionDate { get; set; }

    public double? PredictionScore { get; set; }

    public required double PredictedSalePrice { get; init; }

    public required DateTime CreateDate { get; set; }

    public required DateTime LastUpdateDate { get; set; }

    public required bool Active { get; set; }

    public Miner Miner { get; init; } = null!;

    public Validator? Validator { get; init; }

    public PropertyValuation PropertyValuation { get; init; } = null!;
}