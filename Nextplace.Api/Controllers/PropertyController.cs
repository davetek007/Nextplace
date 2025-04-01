using System.Security.Cryptography;
using Nextplace.Api.Db;
using Nextplace.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using PropertyPrediction = Nextplace.Api.Models.PropertyPrediction;
using Property = Nextplace.Api.Models.Property;
using PropertyContext = Nextplace.Api.Db.Property;
using Microsoft.Extensions.Caching.Memory;
using PropertyValuation = Nextplace.Api.Db.PropertyValuation;
using Nextplace.Api.Helpers;
using System.Text;
using PropertyPredictionStats = Nextplace.Api.Models.PropertyPredictionStats;
using Azure.Identity;
using Microsoft.Graph.Models;
using Microsoft.Graph;
using Microsoft.Graph.Users.Item.SendMail;

namespace Nextplace.Api.Controllers;

[Tags("Property APIs")]
[ApiController]
[Route("Properties")]
public class PropertyController(AppDbContext context, IConfiguration config, IMemoryCache cache) : ControllerBase
{
  [HttpGet("Sample", Name = "SampleProperties")]
  [SwaggerOperation("Sample properties per market")]
  public async Task<ActionResult<List<MarketSample>>> SampleProperties([FromQuery] int sampleSize)
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

    await context.SaveLogEntry("SampleProperties", "SampleSize: " + sampleSize, "Information", executionInstanceId);

    sampleSize = Math.Clamp(sampleSize, 1, 500);

    var sqlQuery = $@"
                 with r as (
                   select	id, market, country, longitude, latitude, listingPrice, listingDate, row_number() over (partition by market, country order by newid()) as row
                   from	dbo.Property)
                 select	id, market, country, longitude, latitude, listingPrice, listingDate, cast (0 as bigint) AS propertyId, '' AS nextplaceId, cast (0 as bigint) AS listingId,'' AS city,'' AS state,'' AS zipCode,'' AS address, NULL AS numberOfBeds, NULL AS numberOfBaths, NULL AS squareFeet, NULL AS lotSize, NULL AS yearBuilt,'' AS propertyType, NULL AS lastSaleDate, NULL AS hoaDues, NULL AS saleDate, NULL AS salePrice, getutcdate() AS createDate, getutcdate() AS lastUpdateDate, cast (1 as bit) AS active
                 from	r
                where	row <= {sampleSize};";

    var query = context.Property.FromSqlRaw(sqlQuery);
    var dict = new Dictionary<string, MarketSample>();

    var results = await query.ToListAsync();

    foreach (var result in results)
    {
      var daysOnMarket = (int)(DateTime.UtcNow - result.ListingDate).TotalDays;

      var pi = new PropertyInfo(
        result.Id,
        result.Longitude,
        result.Latitude,
        daysOnMarket,
        result.ListingPrice);

      var key = result.Market.ToLowerInvariant();
      if (!dict.ContainsKey(key))
      {
        dict.Add(key, new MarketSample(result.Market, result.Country, []));
      }

      dict[key].Properties.Add(pi);
    }

    return Ok(dict.Values.ToList());
  }

  [HttpGet("Training", Name = "GetTrainingData")]
  [SwaggerOperation("Get property training data")]
  public async Task<ActionResult<List<Property>>> GetTrainingData()
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

    const int resultCountPerMarket = 500;

    List<Property> properties;
    const string cacheKey = "GetTrainingData";
    if (cache.TryGetValue(cacheKey, out var cachedData))
    {
      properties = (List<Property>)cachedData!;
      await context.SaveLogEntry("GetTrainingData", "Obtained from cache", "Information", executionInstanceId);
    }
    else
    {
      List<PropertyContext> allProperties = [];
      var markets = await context.Market.Select(m => m.Name).ToListAsync();

      foreach (var market in markets)
      {
        var propertiesForMarket = await context.Property
          .Where(p => p.Market == market && p.SalePrice != null)
          .OrderByDescending(p => p.LastUpdateDate)
          .Take(resultCountPerMarket)
          .ToListAsync();

        allProperties.AddRange(propertiesForMarket);
      }

      properties = allProperties.Select(data => new Property(data.Id, data.PropertyId, data.NextplaceId,
                  data.ListingId, data.Longitude, data.Latitude, data.Market, data.City, data.State, data.ZipCode,
                  data.Address, data.ListingDate, data.ListingPrice, data.NumberOfBeds, data.NumberOfBaths,
                  data.SquareFeet, data.LotSize, data.YearBuilt, data.PropertyType, data.LastSaleDate, data.HoaDues,
                  data.SaleDate, data.SalePrice, data.CreateDate, data.LastUpdateDate, data.Active, data.Country)
      { Predictions = [] })
          .ToList();

      await context.SaveLogEntry("GetTrainingData", "Obtained from DB", "Information", executionInstanceId);

      cache.Set(cacheKey, properties, TimeSpan.FromHours(12));
    }

    return Ok(properties);
  }

  [HttpGet("Current", Name = "GetCurrentProperties")]
  [SwaggerOperation("Get current properties")]
  public async Task<ActionResult<List<Property>>> GetCurrentProperties([FromQuery] int? sampleSize, [FromQuery] double? minLatitude, [FromQuery] double? minLongitude, [FromQuery] double? maxLatitude, [FromQuery] double? maxLongitude)
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

    if (sampleSize.HasValue)
    {
      sampleSize = Math.Clamp(sampleSize.Value, 1, 500);
    }

    List<Property> properties;
    var cacheKey = "GetCurrentProperties" + (sampleSize.HasValue ? sampleSize.Value : "") +
                   (minLatitude.HasValue ? minLatitude.Value : "") + (minLongitude.HasValue ? minLongitude.Value : "") +
                   (maxLatitude.HasValue ? maxLatitude.Value : "") + (maxLongitude.HasValue ? maxLongitude.Value : "");

    if (cache.TryGetValue(cacheKey, out var cachedData))
    {
      properties = (List<Property>)cachedData!;
      await context.SaveLogEntry("GetCurrentProperties", "Obtained from cache", "Information", executionInstanceId);
    }
    else
    {
      IQueryable<PropertyContext> query;
      if (sampleSize.HasValue)
      {
        query = context.Property
          .Where(p => p.ListingDate > DateTime.UtcNow.Date.AddDays(-5)).OrderBy(_ => Guid.NewGuid())
          .Take(sampleSize.Value);
      }
      else
      {
        query = context.Property
          .Where(p => p.ListingDate > DateTime.UtcNow.Date.AddDays(-5));
      }

      if (minLatitude.HasValue && minLongitude.HasValue && maxLatitude.HasValue && maxLongitude.HasValue)
      {
        query = query.Where(p =>
          p.Latitude >= minLatitude && p.Latitude <= maxLatitude &&
          p.Longitude >= minLongitude && p.Longitude <= maxLongitude);
      }

      var results = await query.ToListAsync();

      properties = results.Select(data => new Property(data.Id, data.PropertyId, data.NextplaceId,
                  data.ListingId, data.Longitude, data.Latitude, data.Market, data.City, data.State, data.ZipCode,
                  data.Address, data.ListingDate, data.ListingPrice, data.NumberOfBeds, data.NumberOfBaths,
                  data.SquareFeet, data.LotSize, data.YearBuilt, data.PropertyType, data.LastSaleDate, data.HoaDues,
                  data.SaleDate, data.SalePrice, data.CreateDate, data.LastUpdateDate, data.Active, data.Country)
      { Predictions = [] })
          .ToList();

      await context.SaveLogEntry("GetCurrentProperties", "Obtained from DB", "Information", executionInstanceId);

      cache.Set(cacheKey, properties, TimeSpan.FromHours(12));
    }

    return Ok(properties);
  }

  [HttpGet("Valuations", Name = "GetPropertyValuations")]
  [SwaggerOperation("Get property valuation requests")]
  public async Task<ActionResult<List<Models.PropertyValuation>>> GetPropertyValuations()
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

    var results = await context.PropertyValuation.Where(p => p.RequestStatus == "New" && p.Active).ToListAsync();

    Response.Headers.Append("Nextplace-Search-Total-Count", results.Count.ToString());

    await context.SaveLogEntry("GetPropertyValuations", $"{results.Count} properties valuations found", "Information", executionInstanceId);

    var propertyValuations = new List<Models.PropertyValuation>();
    foreach (var data in results)
    {
      var proposedListingPrice = data.EstimatedListingPrice ?? data.ProposedListingPrice;

      var propertyValuation = new Models.PropertyValuation(
        data.Id,
        data.NextplaceId,
        data.Longitude,
        data.Latitude,
        data.City,
        data.State,
        data.ZipCode,
        data.Address,
        data.NumberOfBeds,
        data.NumberOfBaths,
        data.SquareFeet,
        data.LotSize,
        data.YearBuilt,
        data.HoaDues,
        data.PropertyType,
        proposedListingPrice,
        data.CreateDate,
        data.LastUpdateDate,
        data.Active,
        data.Country,
        data.EstimatedListingPrice);

      propertyValuations.Add(propertyValuation);
    }

    return Ok(propertyValuations);
  }

  [HttpPost("Valuation", Name = "PostPropertyValuation")]
  [SwaggerOperation("Post a property valuation request")]
  public async Task<ActionResult> PostPropertyValuation(PostPropertyValuationRequest request)
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

    await context.SaveLogEntry("PostPropertyValuation", "Request: " + JsonConvert.SerializeObject(request), "Information", executionInstanceId);

    var searchString = request.Address + " " + request.City + " " + request.State + " " + request.ZipCode;
    searchString = searchString.Replace(" ", "-");

    float? estimatedListingPrice = null;
    var estimates = await GetEstimatedListingPriceForProperty(searchString);

    if (estimates != null && estimates.Count != 0)
    {
      estimatedListingPrice = estimates.OrderByDescending(e => e.Timestamp).First().Value;
    }

    var dbEntry = new PropertyValuation
    {
      RequestStatus = "New",
      NextplaceId = $"PVR-{Guid.NewGuid()}",
      Active = true,
      Address = request.Address,
      City = request.City,
      Country = request.Country ?? "United States",
      CreateDate = DateTime.UtcNow,
      LastUpdateDate = DateTime.UtcNow,
      Latitude = request.Latitude,
      Longitude = request.Longitude,
      NumberOfBaths = request.NumberOfBaths,
      NumberOfBeds = request.NumberOfBeds,
      State = request.State,
      ZipCode = request.ZipCode,
      YearBuilt = request.YearBuilt,
      HoaDues = request.HoaDues,
      RequestorEmailAddress = request.RequestorEmailAddress,
      PropertyType = request.PropertyType,
      ProposedListingPrice = request.ProposedListingPrice,
      EstimatedListingPrice = estimatedListingPrice
    };

    context.PropertyValuation.Add(dbEntry);

    await context.SaveChangesAsync();

    await context.SaveLogEntry("PostPropertyValuation", $"Saving property valuation {dbEntry.NextplaceId} to DB", "Information", executionInstanceId);

    return CreatedAtAction(nameof(PostPropertyValuation), new { id = dbEntry.NextplaceId }, dbEntry);
  }

  [HttpPost("Share", Name = "PostPropertyShare")]
  [SwaggerOperation("Post a property share request")]
  public async Task<ActionResult> PostPropertyShare(PostPropertyShareRequest request)
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

    await context.SaveLogEntry("PostPropertyShare", "Request: " + JsonConvert.SerializeObject(request), "Information", executionInstanceId);

    var property =
      await context.Property.FirstOrDefaultAsync(p => p.NextplaceId == request.NextplaceId && p.Active);

    if (property == null)
    {
      await context.SaveLogEntry("PostPropertyShare", $"Property with NextplaceId {request.NextplaceId} not found", "Warning", executionInstanceId);
      return StatusCode(404);
    }

    var shareRef = Guid.NewGuid().ToString();

    var dbEntry = new PropertyShare
    {
      PropertyId = property.Id,
      Active = true,
      SenderEmailAddress = request.SenderEmailAddress,
      ReceiverEmailAddress = request.ReceiverEmailAddress,
      Message = request.Message,
      CreateDate = DateTime.UtcNow,
      LastUpdateDate = DateTime.UtcNow,
      ShareRef = shareRef,
      ViewCount = 0
    };

    context.PropertyShare.Add(dbEntry);

    await context.SaveChangesAsync();

    await SendPropertyShareEmail(request.NextplaceId, shareRef, request.SenderEmailAddress, request.ReceiverEmailAddress, request.Message);

    await context.SaveLogEntry("PostPropertyShare", $"Saving property share for {request.NextplaceId} to DB", "Information", executionInstanceId);

    return Ok();
  }

  [HttpPost("Share/View", Name = "PostPropertyShareView")]
  [SwaggerOperation("Post a property share view request")]
  public async Task<ActionResult> PostPropertyShareView(PostPropertyShareViewRequest request)
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

    await context.SaveLogEntry("PostPropertyShareView", "Request: " + JsonConvert.SerializeObject(request), "Information", executionInstanceId);

    var propertyShare =
      await context.PropertyShare.FirstOrDefaultAsync(p => p.ShareRef == request.ShareRef && p.Active);

    if (propertyShare == null)
    {
      await context.SaveLogEntry("PostPropertyShareView", $"Property Share with Share Ref {request.ShareRef} not found", "Warning", executionInstanceId);
      return StatusCode(404);
    }

    propertyShare.ViewCount++;
    propertyShare.LastUpdateDate = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return Ok();
  }

  public class Estimate
  {
    [JsonProperty("T")]
    public long Timestamp { get; set; }

    [JsonProperty("V")]
    public int Value { get; set; }
  }

  private async Task<List<Estimate>?> GetEstimatedListingPriceForProperty(string searchString)
  {
    const int maxRequestsPerSecond = 3;
    string? responseBody = null;
    var url = config["ZillowApiUrl"];
    using var httpClient = new HttpClient();
    
    var apiKey = await new AkvHelper(config).GetSecretAsync("ZillowApiKey");

    httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", config["ZillowApiHost"]);
    httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);

    var delayBetweenRequests = 1000 / maxRequestsPerSecond;

    try
    {
      var retryCount = 0;
      const int maxRetries = 3;
      bool shouldRetry;

      do
      {
        shouldRetry = false;
        var response = await httpClient.GetAsync(url + searchString);

        if (response.StatusCode == (System.Net.HttpStatusCode)429)
        {
          retryCount++;

          if (response.Headers.TryGetValues("Retry-After", out var retryAfterValues))
          {
            if (int.TryParse(retryAfterValues.FirstOrDefault(), out var retryAfterSeconds))
            {
              await Task.Delay(retryAfterSeconds * 1000);
            }
          }
          else
          {
            await Task.Delay(1000 * retryCount);
          }

          shouldRetry = retryCount < maxRetries;
        }
        else
        {
          response.EnsureSuccessStatusCode();
          responseBody = await response.Content.ReadAsStringAsync();

          await Task.Delay(delayBetweenRequests);
        }
      }
      while (shouldRetry);

      if (responseBody is null or "{}")
      {
        return null;
      }

      var rootObject = JsonConvert.DeserializeObject<List<Estimate>>(responseBody);
      return rootObject;
    }
    catch (Exception ex)
    {
      if (!string.IsNullOrWhiteSpace(responseBody))
      {
        throw new Exception($"Error received from API call: {responseBody}", ex);
      }

      throw;
    }
  }

  [HttpGet("SalesTimeSeries", Name = "SalesTimeSeries")]
  [SwaggerOperation("Get sales time series")]
  public async Task<ActionResult<List<Property>>> SalesTimeSeries([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int minimumPredictionCount)
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

    object data;
    var cacheKey = $"SalesTimeSeries{startDate:s}{endDate:s}{minimumPredictionCount}";

    if (cache.TryGetValue(cacheKey, out var cachedData))
    {
      data = cachedData!;
      await context.SaveLogEntry("SalesTimeSeries", "Obtained from cache", "Information", executionInstanceId);
    }
    else
    {
      data = await context.Property.Where(p => p.SaleDate >= startDate && p.SaleDate <= endDate
                                                                       && p.PredictionStats!.Any() && p.PredictionStats!.First().NumPredictions >=
                                                                       minimumPredictionCount)
        .GroupBy(p => p.SaleDate!.Value.Date)
        .Select(g => new { SaleDate = g.Key, Count = g.Count() }).ToListAsync();

      cache.Set(cacheKey, data, TimeSpan.FromHours(12));
    }

    return Ok(data);
  }

  [HttpGet("MarketPerformance", Name = "MarketPerformance")]
  [SwaggerOperation("Get market performance")]
  public async Task<ActionResult<List<Property>>> MarketPerformance([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int minimumPredictionCount)
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

    object data;
    var cacheKey = $"MarketPerformance{startDate:s}{endDate:s}{minimumPredictionCount}";

    if (cache.TryGetValue(cacheKey, out var cachedData))
    {
      data = cachedData!;
      await context.SaveLogEntry("MarketPerformance", "Obtained from cache", "Information", executionInstanceId);
    }
    else
    {
      data = await context.Property.Where(p => p.SaleDate >= startDate && p.SaleDate <= endDate
                                                                       && p.PredictionStats!.Any() &&
                                                                       p.PredictionStats!.First().NumPredictions >=
                                                                       minimumPredictionCount)
        .GroupBy(p => p.Market)
        .Select(g => new { Market = g.Key, Count = g.Count() }).ToListAsync();

      cache.Set(cacheKey, data, TimeSpan.FromHours(12));
    }

    return Ok(data);
  }

  [HttpGet("Search", Name = "SearchProperties")]
  [SwaggerOperation("Search for properties")]
  public async Task<ActionResult<List<Property>>> SearchProperties([FromQuery] PropertyFilter filter)
  {
    filter.TopPredictionCount = Math.Clamp(filter.TopPredictionCount, 0, 10);
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;
    await context.SaveLogEntry("SearchProperties", "Filter: " + JsonConvert.SerializeObject(filter), "Information", executionInstanceId);

    filter.ItemsPerPage = Math.Clamp(filter.ItemsPerPage, 1, 500);

    var query = context.Property
      .Include(tg => tg.EstimateStats)
      .Include(tg => tg.PredictionStats)
      .AsQueryable();

    query = ApplyDateFilters(query, filter);
    query = ApplyStringFilters(query, filter);
    query = ApplyMarketFilter(query, filter);
    query = ApplyCoordinateFilters(query, filter);
    query = ApplyAwaitingResultFilter(query, filter);
    query = ApplySortOrder(query, filter.SortOrder);
    query = ApplyMinPredictionsFilter(query, filter);

    var totalCount = await query.CountAsync();
    Response.Headers.Append("Nextplace-Search-Total-Count", totalCount.ToString());

    await context.SaveLogEntry("SearchProperties", $"{totalCount} properties found", "Information", executionInstanceId);

    query = query.Skip(filter.PageIndex * filter.ItemsPerPage).Take(filter.ItemsPerPage);

    var properties = await GetProperties(query, filter);
    return Ok(properties);
  }

  [HttpGet("{nextplaceId}", Name = "GetProperty")]
  [SwaggerOperation("Get for property by ID")]
  public async Task<IActionResult> GetProperty([SwaggerParameter("Nextplace ID", Required = true)][FromRoute] string nextplaceId)
  {
    var executionInstanceId = HttpContext.Items["executionInstanceId"]?.ToString()!;

    var property = await context.Property
          .Include(tg => tg.EstimateStats)
          .Include(tg => tg.Images)
          .Include(tg => tg.PredictionStats)!
          .FirstOrDefaultAsync(tg => tg.NextplaceId == nextplaceId);

    if (property == null)
    {
      await context.SaveLogEntry("GetProperty", "No data", "Warning", executionInstanceId);
      await context.SaveLogEntry("GetProperty", "Completed", "Information", executionInstanceId);
      return NotFound();
    }

    var tg = new Property(
        property.Id,
        property.PropertyId,
        property.NextplaceId,
        property.ListingId,
        property.Longitude,
        property.Latitude,
        property.Market,
        property.City,
        property.State,
        property.ZipCode,
        property.Address,
        property.ListingDate,
        property.ListingPrice,
        property.NumberOfBeds,
        property.NumberOfBaths,
        property.SquareFeet,
        property.LotSize,
        property.YearBuilt,
        property.PropertyType,
        property.LastSaleDate,
        property.HoaDues,
        property.SaleDate,
        property.SalePrice,
        property.CreateDate,
        property.LastUpdateDate,
        property.Active,
        property.Country)
    {
      Predictions = []
    };

    tg.ImageIds = await GetPropertyImageIds(property);
    var e = property.EstimateStats!.MaxBy(e => e.CreateDate);
    if (e != null)
    {
      tg.EstimateStats = new Models.PropertyEstimateStats(e.FirstEstimateDate, e.LastEstimateDate, e.NumEstimates, e.AvgEstimate,
          e.MinEstimate, e.MaxEstimate, e.ClosestEstimate, e.FirstEstimateAmount, e.LastEstimateAmount);
    }

    tg.Predictions = [];

    var pps = property.PredictionStats!.FirstOrDefault();

    if (pps != null)
    {
      var json = pps.Top10Predictions;
      var predictionEntries = JsonConvert.DeserializeObject<List<dynamic>>(json);

      if (predictionEntries != null)
      {
        for (var i = 0; i < predictionEntries.Count; i++)
        {
          var entry = predictionEntries[i];
          tg.Predictions.Add(new PropertyPrediction(
            minerHotKey: (string)entry.hotKey,
            minerColdKey: (string)entry.coldKey,
            predictionDate: (DateTime)entry.predictionDate,
            predictedSalePrice: (double)entry.predictedSalePrice,
            predictedSaleDate: (DateTime)entry.predictedSaleDate));
        }
      }
    }

    tg.PredictionStats = new PropertyPredictionStats(pps.NumPredictions, pps.AvgPredictedSalePrice,
      pps.MinPredictedSalePrice, pps.MaxPredictedSalePrice);

    return Ok(tg);
  }

  private async Task<List<string>?> GetPropertyImageIds(PropertyContext property)
  {
    if (property.Images != null && property.Images.Count != 0)
    {
      return property.Images.Select(i => i.ImageId).ToList();
    }

    if (property.Address == null || property.City == null || property.State == null || property.ZipCode == null)
    {
      return null;
    }

    try
    {
      var blobHelper = new BlobHelper(config);
      var lookupUrl =
        $"https://www.redfin.com/{property.State}/{property.City}/{property.Address}-{property.ZipCode}/home/{property.PropertyId}".Replace(" ", "-");

      var apiUrl = $"{config["RedfinPropertyImageUrl"]!}{lookupUrl}";

      var apiKey = await new AkvHelper(config).GetSecretAsync("RedfinApiKey");

      var httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", config["RedfinApiHost"]);
      httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);

      var response = await httpClient.GetAsync(apiUrl);
      var json = await response.Content.ReadAsStringAsync();
      response.EnsureSuccessStatusCode();

      var photoUrls = GetFullScreenPhotoUrls(json);

      if (photoUrls == null)
      {
        return null;
      }
      
      var imageIds = new List<string>();
      var propertyImages = new List<PropertyImage>();

      var tasks = photoUrls.Select(async photoUrl =>
      {
        var imageId = GenerateGuidFromSeed(photoUrl).ToString();
        var imageData = await TryDownloadImage(photoUrl);

        if (imageData != null)
        {
          await blobHelper.SaveToBlobStorage(imageData, imageId);

          var propertyImage = new PropertyImage
          {
            PropertyId = property.Id,
            ImageId = imageId,
            Active = true,
            CreateDate = DateTime.UtcNow,
            LastUpdateDate = DateTime.UtcNow
          };

          propertyImages.Add(propertyImage);
        }

        imageIds.Add(imageId);
      });

      await Task.WhenAll(tasks);

      foreach (var propertyImage in propertyImages)
      {
        await context.PropertyImage.AddAsync(propertyImage);
      }

      await context.SaveChangesAsync();

      return imageIds;
    }
    catch
    {
      return null;
    }
  }

  private static async Task<byte[]?> TryDownloadImage(string? imageUrl)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(imageUrl))
      {
        return null;
      }
      using var client = new HttpClient();
      var imageData = await client.GetByteArrayAsync(imageUrl);
      return imageData;
    }
    catch 
    {
      return null;
    }
  }

  private static Guid GenerateGuidFromSeed(string? seed)
  {
    if (string.IsNullOrWhiteSpace(seed))
    {
      return Guid.Empty;
    }
    using var sha256 = SHA256.Create();
    var namespacedSeed = $"Nextplace-RapidAPI-{seed}";

    var seedBytes = Encoding.UTF8.GetBytes(namespacedSeed);
    var hashBytes = sha256.ComputeHash(seedBytes);

    var guidBytes = new byte[16];
    Array.Copy(hashBytes, guidBytes, 16);

    return new Guid(guidBytes);
  }

  private static List<string>? GetFullScreenPhotoUrls(string json)
  {
    var fullScreenPhotoUrls = new List<string>();

    dynamic jsonObject = JsonConvert.DeserializeObject(json)!;

    if (jsonObject.data == null)
    {
      return null;
    }
    
    foreach (var dataItem in jsonObject.data)
    {
      string url = dataItem.photoUrls.fullScreenPhotoUrl;
      fullScreenPhotoUrls.Add(url);
    }

    return fullScreenPhotoUrls;
  }

  private static IQueryable<PropertyContext> ApplyDateFilters(IQueryable<PropertyContext> query, PropertyFilter filter)
  {
    if (filter.ListingStartDate.HasValue)
    {
      query = query.Where(tg => tg.ListingDate >= filter.ListingStartDate.Value);
    }

    if (filter.ListingEndDate.HasValue)
    {
      query = query.Where(tg => tg.ListingDate <= filter.ListingEndDate.Value);
    }

    if (filter.SaleDateStartDate.HasValue)
    {
      query = query.Where(tg => tg.SaleDate >= filter.SaleDateStartDate.Value);
    }

    if (filter.SaleDateEndDate.HasValue)
    {
      query = query.Where(tg => tg.SaleDate <= filter.SaleDateEndDate.Value);
    }

    if (filter.LastUpdateDate.HasValue)
    {
      query = query.Where(tg => tg.LastUpdateDate > filter.LastUpdateDate.Value);
    }

    return query;
  }

  private static IQueryable<PropertyContext> ApplyStringFilters(IQueryable<PropertyContext> query, PropertyFilter filter)
  {
    if (!string.IsNullOrWhiteSpace(filter.FilterString))
    {
      query = query.Where(tgp =>
          tgp.City != null && tgp.City.Contains(filter.FilterString) ||
          tgp.State != null && tgp.State.Contains(filter.FilterString) ||
          tgp.ZipCode != null && tgp.ZipCode.Contains(filter.FilterString) ||
          tgp.Address != null && tgp.Address.Contains(filter.FilterString) ||
          tgp.Country != null && tgp.Country.Contains(filter.FilterString));
    }

    return query;
  }

  private static IQueryable<PropertyContext> ApplyMarketFilter(IQueryable<PropertyContext> query, PropertyFilter filter)
  {
    if (!string.IsNullOrWhiteSpace(filter.Market))
    {
      query = query.Where(tgp =>
          tgp.Market == filter.Market);
    }

    return query;
  }

  private static IQueryable<PropertyContext> ApplyCoordinateFilters(IQueryable<PropertyContext> query, PropertyFilter filter)
  {
    if (filter is { PropertyMinLatitude: not null, PropertyMaxLatitude: not null })
    {
      query = query.Where(tgp => tgp.Latitude >= filter.PropertyMinLatitude && tgp.Latitude <= filter.PropertyMaxLatitude);
    }

    if (filter is { PropertyMinLongitude: not null, PropertyMaxLongitude: not null })
    {
      query = query.Where(tgp => tgp.Longitude >= filter.PropertyMinLongitude && tgp.Longitude <= filter.PropertyMaxLongitude);
    }

    return query;
  }

  private static IQueryable<PropertyContext> ApplyAwaitingResultFilter(IQueryable<PropertyContext> query, PropertyFilter filter)
  {
    if (filter.AwaitingResult.HasValue)
    {
      query = filter.AwaitingResult.Value
          ? query.Where(tg => tg.SaleDate == null)
          : query.Where(tg => tg.SaleDate != null);
    }

    return query;
  }

  private static IQueryable<PropertyContext> ApplySortOrder(IQueryable<PropertyContext> query, string sortOrder)
  {
    return sortOrder switch
    {
      "id_asc" => query.OrderBy(tg => tg.Id),
      "id_desc" => query.OrderByDescending(tg => tg.Id),
      "listingdate_asc" => query.OrderBy(tg => tg.ListingDate),
      "listingdate_desc" => query.OrderByDescending(tg => tg.ListingDate),
      "longitude_asc" => query.OrderBy(tgp => tgp.Longitude),
      "longitude_desc" => query.OrderByDescending(tgp => tgp.Longitude),
      "latitude_asc" => query.OrderBy(tgp => tgp.Latitude),
      "latitude_desc" => query.OrderByDescending(tgp => tgp.Latitude),
      "market_asc" => query.OrderBy(tgp => tgp.Market),
      "market_desc" => query.OrderByDescending(tgp => tgp.Market),
      _ => query.OrderByDescending(tg => tg.ListingDate)
    };
  }

  private static IQueryable<PropertyContext> ApplyMinPredictionsFilter(IQueryable<PropertyContext> query, PropertyFilter filter)
  {
    if (filter.MinPredictions.HasValue)
    {
      var minPredictions = Math.Clamp(filter.MinPredictions.Value, 0, 50);

      query = query.Where(tg =>
        tg.PredictionStats!.Any() && tg.PredictionStats!.First().NumPredictions >= minPredictions);
    }

    return query;
  }

  private async Task SendPropertyShareEmail(string nextplaceId, string shareRef, string senderEmailAddress, string receiverEmailAddress, string emailMessage)
  {
    var propertySharePage = config["PropertySharePage"]!;

    var akv = new AkvHelper(config);

    var emailTenantId = await akv.GetSecretAsync("EmailTenantId");
    var emailClientSecret = await akv.GetSecretAsync("EmailClientSecret");
    var emailClientId = await akv.GetSecretAsync("EmailClientId");

    var clientSecretCredential = new ClientSecretCredential(emailTenantId, emailClientId, emailClientSecret);

    var graphClient = new GraphServiceClient(clientSecretCredential);

    var message = new SendMailPostRequestBody
    {
      Message = new Message
      {
        Subject = "A property has been shared",
        Body = new ItemBody
        {
          ContentType = BodyType.Html,
          Content = EmailContent.PropertyShared(propertySharePage, nextplaceId, shareRef, senderEmailAddress, receiverEmailAddress, emailMessage)
        },
        ToRecipients = new List<Recipient>
        {
          new()
          {
            EmailAddress = new EmailAddress
            {
              Address = receiverEmailAddress
            }
          }
        },
        CcRecipients = new List<Recipient>
        {
          new()
          {
            EmailAddress = new EmailAddress
            {
              Address = senderEmailAddress
            }
          }
        }
      }
    };

    EmailContent.AddHeaderImage(message.Message);

    await graphClient.Users["admin@nextplace.ai"].SendMail.PostAsync(message);
  }

  internal static async Task<List<Property>> GetProperties(IQueryable<PropertyContext> query, PropertyFilter filter)
  {
    var properties = new List<Property>();

    var results = await query.ToListAsync();

    foreach (var data in results)
    {
      var property = new Property(
          data.Id,
          data.PropertyId,
          data.NextplaceId,
          data.ListingId,
          data.Longitude,
          data.Latitude,
          data.Market,
          data.City,
          data.State,
          data.ZipCode,
          data.Address,
          data.ListingDate,
          data.ListingPrice,
          data.NumberOfBeds,
          data.NumberOfBaths,
          data.SquareFeet,
          data.LotSize,
          data.YearBuilt,
          data.PropertyType,
          data.LastSaleDate,
          data.HoaDues,
          data.SaleDate,
          data.SalePrice,
          data.CreateDate,
          data.LastUpdateDate,
          data.Active,
          data.Country)
      {
        Predictions = []
      };
      
      var e = data.EstimateStats!.MaxBy(e => e.CreateDate);
      if (e != null)
      {
        property.EstimateStats = new Models.PropertyEstimateStats(e.FirstEstimateDate, e.LastEstimateDate, e.NumEstimates, e.AvgEstimate,
            e.MinEstimate, e.MaxEstimate, e.ClosestEstimate, e.FirstEstimateAmount, e.LastEstimateAmount);
      }

      property.Predictions = [];

      var pps = data.PredictionStats!.FirstOrDefault();
      
      if (pps != null)
      {
        var json = pps.Top10Predictions;
        var predictionEntries = JsonConvert.DeserializeObject<List<dynamic>>(json);

        if (predictionEntries != null)
        {
          for (var i = 0; i < predictionEntries.Count; i++)
          {
            if (i > filter.TopPredictionCount)
            {
              break;
            }
            
            var entry = predictionEntries[i];
            property.Predictions.Add(new PropertyPrediction(
              minerHotKey: (string)entry.hotKey,
              minerColdKey: (string)entry.coldKey,
              predictionDate: (DateTime)entry.predictionDate,
              predictedSalePrice: (double)entry.predictedSalePrice,
              predictedSaleDate: (DateTime)entry.predictedSaleDate));
          }
        }

        property.PredictionStats = new PropertyPredictionStats(pps.NumPredictions, pps.AvgPredictedSalePrice,
          pps.MinPredictedSalePrice, pps.MaxPredictedSalePrice);
      }

      properties.Add(property);
    }

    return properties;
  }
}