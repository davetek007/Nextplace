using Newtonsoft.Json;

namespace Nextplace.Functions.Models.SyncPropertyEstimates;

public class Estimate
{
    [JsonProperty("T")]
    public long Timestamp { get; set; }
    
    [JsonProperty("V")]
    public int Value { get; set; } 
}
