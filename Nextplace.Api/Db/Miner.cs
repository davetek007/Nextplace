using Microsoft.AspNetCore.Datasync.EFCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Db;

public sealed class Miner : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    [MaxLength(100)]
    public required string HotKey { get; init; }

    [MaxLength(100)]
    public required string ColdKey { get; init; }

    public required double Incentive { get; set; }

    public required int Uid { get; set; }

    public required DateTime CreateDate { get; set; }

    public required DateTime LastUpdateDate { get; set; }

    public required bool Active { get; set; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<PropertyValuationPrediction>? ValuationPredictions { get; init; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<MinerScore>? Scores { get; init; }
}