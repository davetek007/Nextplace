using Microsoft.AspNetCore.Datasync.EFCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Db;

public sealed class Property : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    public required double Longitude { get; init; }

    public required double Latitude { get; init; }

    [MaxLength(450)]
    public required string Market { get; init; }

    [MaxLength(450)]
    public required string City { get; init; }

    [MaxLength(450)]
    public required string State { get; init; }

    [MaxLength(450)]
    public required string ZipCode { get; init; }

    [MaxLength(450)]
    public required string Address { get; init; }

    public required DateTime ListingDate { get; init; }

    public required double ListingPrice { get; init; }

    public required DateTime? SaleDate { get; init; }

    public required double? SalePrice { get; init; }

    public required DateTime LastUpdateDate { get; init; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<PropertyPrediction>? Predictions { get; init; }
}