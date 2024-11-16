using Microsoft.AspNetCore.Datasync.EFCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.OData.Edm;

namespace Nextplace.Api.Db;

public sealed class Validator : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    [MaxLength(450)]
    public required string IpAddress { get; init; }

    [MaxLength(450)]
    public required string HotKey { get; init; }

    [MaxLength(450)]
    public string? AppVersion { get; set; }

    [MaxLength(450)]
    public required string ColdKey { get; init; }

    public required DateTime LastUpdateDate { get; set; }

    public bool Active { get; set; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<PropertyPrediction>? Predictions { get; init; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<PropertyValuationPrediction>? ValuationPredictions { get; init; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<MinerScore>? MinerScores { get; init; }
}