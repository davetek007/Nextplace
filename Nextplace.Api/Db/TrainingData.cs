using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Api.Db;

public sealed class TrainingData : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    [MaxLength(450)]
    public required string PropertyId { get; init; }

    [MaxLength(450)]
    public required string NextplaceId { get; init; }

    public string? ListingId { get; init; }

    public required double Longitude { get; init; }

    public required double Latitude { get; init; }

    [MaxLength(450)]
    public required string Market { get; init; }

    [MaxLength(450)]
    public string? City { get; init; }

    [MaxLength(450)]
    public string? State { get; init; }

    [MaxLength(450)]
    public string? ZipCode { get; init; }

    [MaxLength(450)]
    public string? Address { get; init; }

    public DateTime? ListingDate { get; init; }

    public double? ListingPrice { get; init; }

    public int? NumberOfBeds { get; init; }

    public double? NumberOfBaths { get; init; }

    public int? SquareFeet { get; init; }

    public long? LotSize { get; init; }

    public int? YearBuilt { get; init; }

    [MaxLength(450)]
    public required string PropertyType { get; init; } = null!;

    public DateTime? LastSaleDate { get; init; }

    public int? HoaDues { get; init; }

    public DateTime? SaleDate { get; init; }

    public double? SalePrice { get; init; }

    public DateTime? CreateDate { get; init; }

    public DateTime? LastUpdateDate { get; init; }

    public bool? Active { get; init; }
}