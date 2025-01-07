namespace Nextplace.Api.Models;

public class PredictionFilter
{
  public int PageIndex { get; set; } = 0;

  public int ItemsPerPage { get; set; } = 10;

  public string SortOrder { get; set; } = string.Empty;

  public DateTime? StartDate { get; set; } = null;

  public DateTime? EndDate { get; set; } = null;

  public string? MinerHotKey { get; set; } = null;

  public string? MinerColdKey { get; set; } = null;

  public long? PropertyId { get; set; } = null;
}