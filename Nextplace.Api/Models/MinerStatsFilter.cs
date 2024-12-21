namespace Nextplace.Api.Models;

public class MinerStatsFilter
{
    public string? MinerHotKey { get; set; } = null;

    public string? ValidatorHotKey { get; set; } = null;

    public DateTime? StartDate { get; set; } = null;

    public DateTime? EndDate { get; set; } = null;
}