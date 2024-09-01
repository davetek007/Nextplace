namespace Nextplace.Api.Models;

public class PredictionFilter
{
    public int PageIndex { get; set; } = 0;

    public int ItemsPerPage { get; set; } = 10;

    public string SortOrder { get; set; } = string.Empty;

    public DateTime? ListingStartDate { get; set; } = null;

    public DateTime? ListingEndDate { get; set; } = null;

    public string? FilterString { get; set; } = null;

    public string? MinerHotKey { get; set; } = null;

    public string? MinerColdKey { get; set; } = null;

    public bool? AwaitingResult { get; set; } = null;

    public string? Market { get; set; } = null;

    public double? PropertyMinLongitude { get; set; } = null;

    public double? PropertyMaxLongitude { get; set; } = null;

    public double? PropertyMinLatitude { get; set; } = null;

    public double? PropertyMaxLatitude { get; set; } = null;

    public long? PropertyId { get; set; } = null;
}