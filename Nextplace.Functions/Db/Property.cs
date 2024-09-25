﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Functions.Db;

public sealed class Property : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    [MaxLength(450)]
    public required long PropertyId { get; init; }

    [MaxLength(450)]
    public required string NextplaceId { get; init; }

    [MaxLength(450)]
    public required long ListingId { get; init; }

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

    public required DateTime ListingDate { get; init; }

    public required double ListingPrice { get; init; }

    public int? NumberOfBeds { get; init; }

    public double? NumberOfBaths { get; init; }

    public int? SquareFeet { get; init; }

    public long? LotSize { get; init; }

    public int? YearBuilt { get; init; }

    [MaxLength(450)]
    public required string PropertyType { get; init; } = null!;

    public DateTime? LastSaleDate { get; init; }

    public int? HoaDues { get; init; }

    public DateTime? SaleDate { get; set; }

    public double? SalePrice { get; set; }

    public required DateTime CreateDate { get; init; }

    public required DateTime LastUpdateDate { get; set; }

    public required bool Active { get; init; }
}