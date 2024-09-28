using Newtonsoft.Json;

public class Root
{
    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("total_count_filtered")]
    public TotalCountFiltered TotalCountFiltered { get; set; }

    [JsonProperty("total_count_unfiltered")]
    public object TotalCountUnfiltered { get; set; }

    [JsonProperty("items")]
    public List<Item> Items { get; set; }
}

public class TotalCountFiltered
{
    [JsonProperty("count")]
    public int Count { get; set; }
}

public class Item
{
    [JsonProperty("active")]
    public bool Active { get; set; }

    [JsonProperty("hotkey")]
    public Hotkey Hotkey { get; set; }

    [JsonProperty("coldkey")]
    public Coldkey Coldkey { get; set; }

    [JsonProperty("validator_permit")]
    public bool ValidatorPermit { get; set; }

    [JsonProperty("daily_reward")]
    public string DailyReward { get; set; }

    [JsonProperty("registered_at_block")]
    public int RegisteredAtBlock { get; set; }

    [JsonProperty("is_immunity_period")]
    public bool IsImmunityPeriod { get; set; }

    [JsonProperty("axon_info")]
    public AxonInfo? AxonInfo { get; set; }

    [JsonProperty("rank")]
    public int Rank { get; set; }

    [JsonProperty("is_child_key")]
    public bool IsChildKey { get; set; }

    [JsonProperty("updated")]
    public int Updated { get; set; }

    [JsonProperty("block_number")]
    public int BlockNumber { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonProperty("subnet_id")]
    public int SubnetId { get; set; }

    [JsonProperty("neuron_id")]
    public int NeuronId { get; set; }

    [JsonProperty("stake")]
    public double Stake { get; set; }

    [JsonProperty("trust")]
    public double Trust { get; set; }

    [JsonProperty("validator_trust")]
    public double ValidatorTrust { get; set; }

    [JsonProperty("consensus")]
    public double Consensus { get; set; }

    [JsonProperty("incentive")]
    public double Incentive { get; set; }

    [JsonProperty("dividends")]
    public double Dividends { get; set; }

    [JsonProperty("emission")]
    public long Emission { get; set; }
}

public class Hotkey
{
    [JsonProperty("ss58")]
    public string Ss58 { get; set; }

    [JsonProperty("hex")]
    public string Hex { get; set; }
}

public class Coldkey
{
    [JsonProperty("ss58")]
    public string Ss58 { get; set; }

    [JsonProperty("hex")]
    public string Hex { get; set; }
}

public class AxonInfo
{
    [JsonProperty("block")]
    public int Block { get; set; }

    [JsonProperty("ip")]
    public string Ip { get; set; }

    [JsonProperty("ipType")]
    public int IpType { get; set; }

    [JsonProperty("placeholder1")]
    public int Placeholder1 { get; set; }

    [JsonProperty("placeholder2")]
    public int Placeholder2 { get; set; }

    [JsonProperty("port")]
    public int Port { get; set; }

    [JsonProperty("protocol")]
    public int Protocol { get; set; }

    [JsonProperty("version")]
    public int Version { get; set; }
}
