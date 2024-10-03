using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Functions.Db;

public sealed class PropertyEstimateStats : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    public required long PropertyId { get; init; }

    public required DateTime FirstEstimateDate { get; set; }

    public required DateTime LastEstimateDate { get; set; }

    public required double FirstEstimateAmount { get; set; }

    public required double LastEstimateAmount { get; set; }

    public required int NumEstimates { get; set; }

    public required double MinEstimate { get; set; }

    public required double MaxEstimate { get; set; }

    public required double AvgEstimate { get; set; }

    public required double ClosestEstimate { get; set; }

    public required DateTime CreateDate { get; set; }

    public required DateTime LastUpdateDate { get; set; }

    public required bool Active { get; set; }

    public Property Property { get; init; } = null!;
}