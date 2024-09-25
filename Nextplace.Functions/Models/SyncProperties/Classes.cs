using Newtonsoft.Json;

namespace Nextplace.Functions.Models.SyncProperties;

public class PropertyListing
{
    [JsonProperty("data")]
    public List<HomeDataWrapper>? Data { get; set; }

    [JsonProperty("meta")]
    public required MetaData Meta { get; set; }
}

public class HomeDataWrapper
{
    [JsonProperty("homeData")]
    public required HomeData HomeData { get; set; }
}

public class HomeData
{
    [JsonProperty("propertyId")]
    public required string PropertyId { get; set; }

    [JsonProperty("listingId")]
    public required string ListingId { get; set; }

    [JsonProperty("propertyType")]
    public required int PropertyType { get; set; }

    [JsonProperty("beds")]
    public int? Beds { get; set; }

    [JsonProperty("baths")]
    public double? Baths { get; set; }

    [JsonProperty("priceInfo")]
    public required PriceInfo PriceInfo { get; set; }

    [JsonProperty("sqftInfo")]
    public SqftInfo? SqftInfo { get; set; }

    [JsonProperty("daysOnMarket")]
    public required DaysOnMarket DaysOnMarket { get; set; }

    [JsonProperty("yearBuilt")]
    public YearBuilt? YearBuilt { get; set; }

    [JsonProperty("lotSize")]
    public LotSize? LotSize { get; set; }

    [JsonProperty("hoaDues")]
    public HoaDues? HoaDues { get; set; }

    [JsonProperty("lastSaleData")]
    public LastSaleData? LastSaleData { get; set; }

    [JsonProperty("addressInfo")]
    public required AddressInfo AddressInfo { get; set; }
}

public class PriceInfo
{
    [JsonProperty("amount")]
    public required double Amount { get; set; }
}

public class SqftInfo
{
    [JsonProperty("amount")]
    public int? Amount { get; set; }
}

public class DaysOnMarket
{
    [JsonProperty("listingAddedDate")]
    public DateTime ListingAddedDate { get; set; }
}

public class YearBuilt
{
    [JsonProperty("yearBuilt")]
    public int? Year { get; set; }
}

public class LotSize
{
    [JsonProperty("amount")]
    public int? Amount { get; set; }
}

public class HoaDues
{
    [JsonProperty("amount")]
    public int? Amount { get; set; }
}

public class LastSaleData
{
    [JsonProperty("lastSoldDate")]
    public DateTime ?LastSoldDate { get; set; }
}

public class AddressInfo
{
    [JsonProperty("centroid")]
    public required CentroidWrapper Centroid { get; set; }
    
    [JsonProperty("formattedStreetLine")]
    public string? FormattedStreetLine { get; set; }

    [JsonProperty("city")]
    public string? City { get; set; }

    [JsonProperty("state")]
    public string? State { get; set; }

    [JsonProperty("zip")]
    public string? Zip { get; set; }
}

public class LatLng
{
    [JsonProperty("latitude")]
    public double Latitude { get; set; }

    [JsonProperty("longitude")]
    public double Longitude { get; set; }
}

public class CentroidWrapper
{
    [JsonProperty("centroid")]
    public required LatLng Centroid { get; set; }
}

public class MetaData
{
    [JsonProperty("moreData")]
    public bool MoreData { get; set; }
}
