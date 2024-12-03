using Newtonsoft.Json;

public class RootObject
{
    [JsonProperty("pagination")]
    public Pagination Pagination { get; set; }

    [JsonProperty("data")]
    public List<DataItem> Data { get; set; }
}

public class Pagination
{
    [JsonProperty("current_page")]
    public int CurrentPage { get; set; }

    [JsonProperty("per_page")]
    public int PerPage { get; set; }

    [JsonProperty("total_items")]
    public int TotalItems { get; set; }

    [JsonProperty("total_pages")]
    public int TotalPages { get; set; }

    [JsonProperty("next_page")]
    public int? NextPage { get; set; }

    [JsonProperty("prev_page")]
    public int? PrevPage { get; set; }
}

public class DataItem
{
    [JsonProperty("hotkey")]
    public Key Hotkey { get; set; }

    [JsonProperty("coldkey")]
    public Key Coldkey { get; set; }

    [JsonProperty("netuid")]
    public int Netuid { get; set; }

    [JsonProperty("uid")]
    public int Uid { get; set; }

    [JsonProperty("block_number")]
    public int BlockNumber { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

    [JsonProperty("stake")]
    public string Stake { get; set; }

    [JsonProperty("trust")]
    public string Trust { get; set; }

    [JsonProperty("validator_trust")]
    public double ValidatorTrust { get; set; }

    [JsonProperty("consensus")]
    public string Consensus { get; set; }

    [JsonProperty("incentive")]
    public double Incentive { get; set; }

    [JsonProperty("dividends")]
    public string Dividends { get; set; }

    [JsonProperty("emission")]
    public string Emission { get; set; }

    [JsonProperty("active")]
    public bool Active { get; set; }

    [JsonProperty("validator_permit")]
    public bool ValidatorPermit { get; set; }

    [JsonProperty("updated")]
    public int Updated { get; set; }

    [JsonProperty("daily_reward")]
    public string DailyReward { get; set; }

    [JsonProperty("registered_at_block")]
    public int RegisteredAtBlock { get; set; }

    [JsonProperty("is_immunity_period")]
    public bool IsImmunityPeriod { get; set; }

    [JsonProperty("rank")]
    public int Rank { get; set; }

    [JsonProperty("is_child_key")]
    public bool IsChildKey { get; set; }

    [JsonProperty("axon")]
    public Axon Axon { get; set; }
}

public class Key
{
    [JsonProperty("ss58")]
    public string Ss58 { get; set; }

    [JsonProperty("hex")]
    public string Hex { get; set; }
}

public class Axon
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
