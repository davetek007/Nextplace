using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nextplace.Functions.Db;
using Nextplace.Functions.Helpers;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nextplace.Functions.Models.SyncProperties;

namespace Nextplace.Functions.Functions;

public sealed class SyncProperties(ILoggerFactory loggerFactory, IConfiguration configuration, AppDbContext context)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SyncProperties>(); 

    [Function("SyncProperties")]
    public async Task Run([TimerTrigger("%SyncPropertiesTimerSchedule%")] TimerInfo myTimer)
    {
        var executionInstanceId = Guid.NewGuid().ToString();
        try
        {
            _logger.LogInformation($"SyncProperties executed at: {DateTime.UtcNow}");
            await context.SaveLogEntry("SyncProperties", "Started", "Information", executionInstanceId);

            var hashKey = Encoding.UTF8.GetBytes(await new AkvHelper(configuration).GetSecretAsync("NextPlaceHashKey"));
            var markets = context.Market.Where(m => m.Active).ToList();

            foreach (var market in markets)
            {
                await context.SaveLogEntry("SyncProperties", $"Processing properties for sale in market {market.Name}", "Information", executionInstanceId);
                await SyncPropertiesForSale(market, executionInstanceId, hashKey);

                await context.SaveLogEntry("SyncProperties", $"Processing sold properties in market {market.Name}", "Information", executionInstanceId);
                await SyncSoldProperties(market, executionInstanceId, hashKey);
            }

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation(
                    $"Next timer for SyncProperties is schedule at: {myTimer.ScheduleStatus.Next}");
            }
            await context.SaveLogEntry("SyncProperties", "Completed", "Information", executionInstanceId);
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("SyncProperties", ex, executionInstanceId);
        }
    }

    private async Task SyncSoldProperties(Market market, string executionInstanceId, byte[] hashKey)
    {
        bool moreData;
        var page = 0;

        do
        {
            var apiUrl = $"{configuration["RedfinSoldPropertiesApiUrl"]!}?limit=350&soldWithin=31&regionId={market.ExternalId}&page={++page}";
            var apiKey = await new AkvHelper(configuration).GetSecretAsync("RedfinApiKey");

            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", configuration["RedfinApiHost"]);
            httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);

            var response = await httpClient.GetAsync(apiUrl);
            var json = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var rootObject = JsonConvert.DeserializeObject<PropertyListing>(json)!;

            if (rootObject.Data != null && rootObject.Data.Count != 0)
            {
                await context.SaveLogEntry("SyncProperties", $"Found {rootObject.Data.Count} properties on page {page}", "Information", executionInstanceId);
                foreach (var homeData in rootObject.Data.Select(d => d.HomeData))
                {
                    if (!long.TryParse(homeData.ListingId, out var listingId) || !long.TryParse(homeData.PropertyId, out var propertyId))
                    {
                        continue;
                    }

                    var property = await context.Property.FirstOrDefaultAsync(p => p.ListingId == listingId && p.PropertyId == propertyId);
                    
                    if (property == null)
                    {
                        InsertProperty(listingId, propertyId, homeData, market, hashKey);
                        continue;
                    }
                    
                    var saleDate = homeData.LastSaleData?.LastSoldDate ?? null;
                    
                    if (saleDate == null || saleDate < property.ListingDate)
                    {
                        continue;
                    }
                    
                    var salePrice = homeData.PriceInfo.Amount;
                    
                    property.SaleDate = saleDate;
                    property.SalePrice = salePrice;
                    
                    
                    property.LastUpdateDate = DateTime.UtcNow;
                }

                await context.SaveChangesAsync();
            }
            else
            {
                await context.SaveLogEntry("SyncProperties", $"No properties found on page {page}", "Information", executionInstanceId);
            }

            await context.SaveChangesAsync();
            moreData = rootObject.Meta.MoreData;
        }
        while (moreData);
    }

    private void InsertProperty(long listingId, long propertyId, HomeData homeData, Market market, byte[] hashKey)
    {
        var nextplaceId = GetHash(hashKey, homeData.AddressInfo.FormattedStreetLine, homeData.AddressInfo.Zip);
        var longitude = homeData.AddressInfo.Centroid.Centroid.Longitude;
        var latitude = homeData.AddressInfo.Centroid.Centroid.Latitude;
        var marketName = market.Name;
        var city = homeData.AddressInfo.City;
        var state = homeData.AddressInfo.State;
        var zipCode = homeData.AddressInfo.Zip;
        var address = homeData.AddressInfo.FormattedStreetLine;
        var listingDate = homeData.DaysOnMarket.ListingAddedDate;
        var listingPrice = homeData.PriceInfo.Amount;
        var numberOfBeds = homeData.Beds;
        var numberOfBaths = homeData.Baths;
        var squareFeet = homeData.SqftInfo?.Amount ?? null;
        var lotSize = homeData.LotSize?.Amount ?? null;
        var yearBuilt = homeData.YearBuilt?.Year ?? null;
        var propertyType = homeData.PropertyType.ToString();
        var lastSaleDate = homeData.LastSaleData?.LastSoldDate ?? null;
        var hoaDues = homeData.HoaDues?.Amount ?? null;

        var property = new Property
        {
            PropertyId = propertyId,
            NextplaceId = nextplaceId,
            ListingId = listingId,
            Longitude = longitude,
            Latitude = latitude,
            Market = marketName,
            City = city,
            State = state,
            ZipCode = zipCode,
            Address = address,
            ListingDate = listingDate,
            ListingPrice = listingPrice,
            NumberOfBeds = numberOfBeds,
            NumberOfBaths = numberOfBaths,
            SquareFeet = squareFeet,
            LotSize = lotSize,
            YearBuilt = yearBuilt,
            PropertyType = propertyType,
            LastSaleDate = lastSaleDate,
            HoaDues = hoaDues,
            CreateDate = DateTime.UtcNow,
            LastUpdateDate = DateTime.UtcNow,
            Active = true,
            EstimatesCollected = false
        };

        context.Property.Add(property);
    }

    private async Task SyncPropertiesForSale(Market market, string executionInstanceId, byte[] hashKey)
    {
        bool moreData;
        var page = 0;

        do
        {
            var apiUrl = $"{configuration["RedfinPropertiesForSaleApiUrl"]!}?limit=350&regionId={market.ExternalId}&page={++page}";
            var apiKey = await new AkvHelper(configuration).GetSecretAsync("RedfinApiKey");

            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", configuration["RedfinApiHost"]);
            httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);

            var response = await httpClient.GetAsync(apiUrl);
            var json = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var rootObject = JsonConvert.DeserializeObject<PropertyListing>(json)!;

            if (rootObject.Data != null && rootObject.Data.Count != 0)
            {
                await context.SaveLogEntry("SyncProperties", $"Found {rootObject.Data.Count} properties on page {page}", "Information", executionInstanceId);
                foreach (var homeData in rootObject.Data.Select(d => d.HomeData))
                {
                    if (!long.TryParse(homeData.ListingId, out var listingId) || !long.TryParse(homeData.PropertyId, out var propertyId))
                    {
                        continue;
                    }
                    
                    if (await context.Property.AnyAsync(p => p.ListingId == listingId && p.PropertyId == propertyId))
                    {
                        continue;
                    }

                    InsertProperty(listingId, propertyId, homeData, market, hashKey);
                }
            }
            else
            {
                await context.SaveLogEntry("SyncProperties", $"No properties found on page {page}", "Information", executionInstanceId);
            }
                    
            await context.SaveChangesAsync();
            moreData = rootObject.Meta.MoreData;
        }
        while (moreData);
    }

    private static string GetHash(byte[] hashKey, string? address, string? zip)
    {
        using var hmac = new HMACSHA256(hashKey);

        var messageBytes = Encoding.UTF8.GetBytes($"{address ?? string.Empty}-{zip ?? string.Empty}");
        var hashBytes = hmac.ComputeHash(messageBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}