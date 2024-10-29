using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PropertyFilter
{
    public int PageIndex { get; set; } = 0;

    public int ItemsPerPage { get; set; } = 10;

    public int TopPredictionCount { get; set; } = 10;

    public int? MinPredictions { get; set; } = 0;

    public string SortOrder { get; set; } = string.Empty;

    public DateTime? ListingStartDate { get; set; } = null;

    public DateTime? ListingEndDate { get; set; } = null;

    public DateTime? LastUpdateDate { get; set; } = null;

    public string? Market { get; set; } = null;

    public double? PropertyMinLongitude { get; set; } = null;

    public double? PropertyMaxLongitude { get; set; } = null;

    public double? PropertyMinLatitude { get; set; } = null;

    public double? PropertyMaxLatitude { get; set; } = null;

    public string? FilterString { get; set; } = null;

    public bool? AwaitingResult { get; set; } = null;
}