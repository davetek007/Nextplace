﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Functions.Db;

public sealed class PropertyValuation : EntityTableData
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public new long Id { get; init; }

  [MaxLength(450)] public required string NextplaceId { get; init; }

  public required double Longitude { get; init; }

  public required double Latitude { get; init; }

  [MaxLength(450)] public string? City { get; init; }

  [MaxLength(450)] public string? State { get; init; }

  [MaxLength(450)] public string? ZipCode { get; init; }

  [MaxLength(450)] public string? Address { get; init; }

  [MaxLength(450)] public string? Country { get; init; }

  public int? NumberOfBeds { get; init; }

  public double? NumberOfBaths { get; init; }

  public int? SquareFeet { get; init; }

  public long? LotSize { get; init; }

  public int? YearBuilt { get; init; }

  public int? HoaDues { get; init; }

  public required int PropertyType { get; init; }

  public required double ProposedListingPrice { get; init; }

  [MaxLength(450)] public required string RequestorEmailAddress { get; init; }

  [MaxLength(450)] public required string RequestStatus { get; set; }

  public required DateTime CreateDate { get; init; }

  public required DateTime LastUpdateDate { get; set; }

  public required bool Active { get; init; }

  // ReSharper disable once CollectionNeverUpdated.Global
  public ICollection<PropertyValuationPrediction>? Predictions { get; init; }
}