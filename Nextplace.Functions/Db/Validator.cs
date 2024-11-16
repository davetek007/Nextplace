using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Functions.Db;

public sealed class Validator : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    [MaxLength(100)]
    public required string HotKey { get; init; }

    [MaxLength(100)]
    public required string ColdKey { get; init; }

    [MaxLength(450)]
    public required string IpAddress { get; set; }

    public required double Incentive { get; set; }

    public required DateTime CreateDate { get; set; }

    public required DateTime LastUpdateDate { get; set; }

    public required bool Active { get; set; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<PropertyValuationPrediction>? ValuationPredictions { get; init; }
}