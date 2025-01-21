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

namespace Nextplace.Api.Controllers;

[Tags("Property APIs")]
[ApiController]
[Route("Properties")]
public class PropertyController(AppDbContext context, IConfiguration config, IMemoryCache cache) : ControllerBase
{
  [HttpGet("Sample", Name = "SampleProperties")]
  [SwaggerOperation("Sample properties per market")]
  public async Task<ActionResult<List<MarketSample>>> Sample([FromQuery] int sampleSize)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      if (!HttpContext.CheckRateLimit(cache, config, "SampleProperties", out var offendingIpAddress))
      {
        await context.SaveLogEntry("SampleProperties", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("SampleProperties", "Started", "Information", executionInstanceId);
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

      Response.AppendCorsHeaders();

      await context.SaveLogEntry("SampleProperties", "Completed", "Information", executionInstanceId);
      return Ok(dict.Values.ToList());
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("SampleProperties", ex, executionInstanceId);
      return StatusCode(500);
    }
  }

  [HttpGet("Training", Name = "GetTrainingData")]
  [SwaggerOperation("Get property training data")]
  public async Task<ActionResult<List<Property>>> GetTrainingData()
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      const int resultCount = 100000;
      if (!HttpContext.CheckRateLimit(cache, config, "GetTrainingData", out var offendingIpAddress))
      {
        await context.SaveLogEntry("GetTrainingData", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("GetTrainingData", "Started", "Information", executionInstanceId);

      List<Property> properties;
      const string cacheKey = "GetTrainingData";
      if (cache.TryGetValue(cacheKey, out var cachedData))
      {
        properties = (List<Property>)cachedData!;
        await context.SaveLogEntry("GetTrainingData", "Obtained from cache", "Information", executionInstanceId);
      }
      else
      {
        var results = await context.Property
            .OrderByDescending(p => p.LastUpdateDate)
            .Take(resultCount).ToListAsync();


        properties = results.Select(data => new Property(data.Id, data.PropertyId, data.NextplaceId,
                    data.ListingId, data.Longitude, data.Latitude, data.Market, data.City, data.State, data.ZipCode,
                    data.Address, data.ListingDate, data.ListingPrice, data.NumberOfBeds, data.NumberOfBaths,
                    data.SquareFeet, data.LotSize, data.YearBuilt, data.PropertyType, data.LastSaleDate, data.HoaDues,
                    data.SaleDate, data.SalePrice, data.CreateDate, data.LastUpdateDate, data.Active, data.Country)
        { Predictions = [] })
            .ToList();

        await context.SaveLogEntry("GetTrainingData", "Obtained from DB", "Information", executionInstanceId);

        cache.Set(cacheKey, properties, TimeSpan.FromHours(12));
      }

      Response.AppendCorsHeaders();

      await context.SaveLogEntry("GetTrainingData", "Completed", "Information", executionInstanceId);
      return Ok(properties);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("GetTrainingData", ex, executionInstanceId);
      return StatusCode(500);
    }
  }

  [HttpGet("Current", Name = "GetCurrentProperties")]
  [SwaggerOperation("Get current properties")]
  public async Task<ActionResult<List<Property>>> GetCurrentProperties([FromQuery] int? sampleSize, [FromQuery] double? minLatitude, [FromQuery] double? minLongitude, [FromQuery] double? maxLatitude, [FromQuery] double? maxLongitude)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      if (!HttpContext.CheckRateLimit(cache, config, "GetCurrentProperties", out var offendingIpAddress))
      {
        await context.SaveLogEntry("GetCurrentProperties", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("GetCurrentProperties", "Started", "Information", executionInstanceId);

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

      Response.AppendCorsHeaders();

      await context.SaveLogEntry("GetCurrentProperties", "Completed", "Information", executionInstanceId);
      return Ok(properties);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("GetCurrentProperties", ex, executionInstanceId);
      return StatusCode(500);
    }
  }

  [HttpGet("Valuations", Name = "GetPropertyValuations")]
  [SwaggerOperation("Get property valuation requests")]
  public async Task<ActionResult<List<Models.PropertyValuation>>> GetPropertyValuations()
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      if (!HttpContext.CheckRateLimit(cache, config, "GetPropertyValuations", out var offendingIpAddress))
      {
        await context.SaveLogEntry("GetPropertyValuations", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("GetPropertyValuations", "Started", "Information", executionInstanceId);

      var results = await context.PropertyValuation.Where(p => p.RequestStatus == "New" && p.Active).ToListAsync();

      Response.Headers.Append("Nextplace-Search-Total-Count", results.Count.ToString());

      Response.AppendCorsHeaders();

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

      await context.SaveLogEntry("GetPropertyValuations", "Completed", "Information", executionInstanceId);
      return Ok(propertyValuations);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("GetPropertyValuations", ex, executionInstanceId);
      return StatusCode(500);
    }
  }

  [HttpPost("Valuation", Name = "PostPropertyValuation")]
  [SwaggerOperation("Post a property valuation request")]
  public async Task<ActionResult> PostPropertyValuation(PostPropertyValuationRequest request)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      if (!HttpContext.CheckRateLimit(cache, config, "PostPropertyValuation", out var offendingIpAddress))
      {
        await context.SaveLogEntry("PostPropertyValuation", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("PostPropertyValuation", "Started", "Information", executionInstanceId);
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
      await context.SaveLogEntry("PostPropertyValuation", "Completed", "Information", executionInstanceId);

      return CreatedAtAction(nameof(PostPropertyValuation), new { id = dbEntry.NextplaceId }, dbEntry);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("PostPropertyValuation", ex, executionInstanceId);
      return StatusCode(500);
    }
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
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      if (!HttpContext.CheckRateLimit(cache, config, "SalesTimeSeries", out var offendingIpAddress))
      {
        await context.SaveLogEntry("SalesTimeSeries", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("SalesTimeSeries", "Started", "Information", executionInstanceId);

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
                && p.Predictions!.Count >=
                minimumPredictionCount)
            .GroupBy(p => p.SaleDate!.Value.Date)
            .Select(g => new { SaleDate = g.Key, Count = g.Count() }).ToListAsync();

        cache.Set(cacheKey, data, TimeSpan.FromHours(12));
      }

      Response.AppendCorsHeaders();

      await context.SaveLogEntry("SalesTimeSeries", "Completed", "Information", executionInstanceId);
      return Ok(data);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("SalesTimeSeries", ex, executionInstanceId);
      return StatusCode(500);
    }
  }

  [HttpGet("MarketPerformance", Name = "MarketPerformance")]
  [SwaggerOperation("Get market performance")]
  public async Task<ActionResult<List<Property>>> MarketPerformance([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int minimumPredictionCount)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      if (!HttpContext.CheckRateLimit(cache, config, "MarketPerformance", out var offendingIpAddress))
      {
        await context.SaveLogEntry("MarketPerformance", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("MarketPerformance", "Started", "Information", executionInstanceId);


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
                && p.Predictions!.Count >=
                minimumPredictionCount)
            .GroupBy(p => p.Market)
            .Select(g => new { Market = g.Key, Count = g.Count() }).ToListAsync();

        cache.Set(cacheKey, data, TimeSpan.FromHours(12));
      }
      Response.AppendCorsHeaders();


      await context.SaveLogEntry("MarketPerformance", "Completed", "Information", executionInstanceId);
      return Ok(data);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("MarketPerformance", ex, executionInstanceId);
      return StatusCode(500);
    }
  }

  [HttpGet("Search", Name = "SearchProperties")]
  [SwaggerOperation("Search for properties")]
  public async Task<ActionResult<List<Property>>> Search([FromQuery] PropertyFilter filter)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      if (!HttpContext.CheckRateLimit(cache, config, "SearchProperties", out var offendingIpAddress))
      {
        await context.SaveLogEntry("SearchProperties", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("SearchProperties", "Started", "Information", executionInstanceId);
      await context.SaveLogEntry("SearchProperties", "Filter: " + JsonConvert.SerializeObject(filter), "Information", executionInstanceId);

      filter.ItemsPerPage = Math.Clamp(filter.ItemsPerPage, 1, 500);

      var query = context.Property
          .Include(tg => tg.Predictions)!.ThenInclude(p => p.Miner)
          .Include(tg => tg.EstimateStats)
          .AsQueryable();

      query = ApplyDateFilters(query, filter);
      query = ApplyStringFilters(query, filter);
      query = ApplyMarketFilter(query, filter);
      query = ApplyCoordinateFilters(query, filter);
      query = ApplyAwaitingResultFilter(query, filter);
      query = ApplyMinPredictionsFilter(query, filter);
      query = ApplySortOrder(query, filter.SortOrder);

      var totalCount = await query.CountAsync();
      Response.Headers.Append("Nextplace-Search-Total-Count", totalCount.ToString());

      Response.AppendCorsHeaders();

      await context.SaveLogEntry("SearchProperties", $"{totalCount} properties found", "Information", executionInstanceId);

      query = query.Skip(filter.PageIndex * filter.ItemsPerPage).Take(filter.ItemsPerPage);

      var properties = await GetProperties(query, filter);

      await context.SaveLogEntry("SearchProperties", "Completed", "Information", executionInstanceId);
      return Ok(properties);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("SearchProperties", ex, executionInstanceId);
      return StatusCode(500);
    }
  }

  [HttpGet("{nextplaceId}", Name = "GetProperty")]
  [SwaggerOperation("Get for property by ID")]
  public async Task<IActionResult> Get([SwaggerParameter("Nextplace ID", Required = true)][FromRoute] string nextplaceId)
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      if (!HttpContext.CheckRateLimit(cache, config, "GetProperty", out var offendingIpAddress))
      {
        await context.SaveLogEntry("GetProperty", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("GetProperty", "Started", "Information", executionInstanceId);
      var property = await context.Property
          .Include(tg => tg.EstimateStats)
          .Include(tg => tg.Images)
          .Include(tg => tg.Predictions)!
          .ThenInclude(propertyPrediction => propertyPrediction.Miner).OrderByDescending(p => p.ListingDate)
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

      if (property.Predictions != null)
      {
        foreach (var prediction in property.Predictions.Where(p => p.Active))
        {
          var tgp = new PropertyPrediction(prediction.Miner.HotKey,
              prediction.Miner.ColdKey, prediction.PredictionDate, prediction.PredictedSalePrice,
              prediction.PredictedSaleDate);

          tg.Predictions.Add(tgp);
        }
      }

      Response.AppendCorsHeaders();

      await context.SaveLogEntry("GetProperty", "Completed", "Information", executionInstanceId);
      return Ok(tg);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("GetProperty", ex, executionInstanceId);
      return StatusCode(500);
    }
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

  [HttpGet("Cities/Trending", Name = "GetTrendingCities")]
  [SwaggerOperation("Get trending cities")]
  public async Task<ActionResult<List<TrendingCity>>> GetTrendingCities()
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      if (!HttpContext.CheckRateLimit(cache, config, "GetTrendingCities", out var offendingIpAddress))
      {
        await context.SaveLogEntry("GetTrendingCities", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("GetTrendingCities", "Started", "Information", executionInstanceId);
      const int resultCount = 200;

      var properties = await context.Property
          .Where(tg => tg.Predictions != null)
          .Include(tg => tg.Predictions)
          .ToListAsync();

      var trendingCities = properties.Where(p => p.City != null)
          .GroupBy(tg => new { tg.City })
          .Select(g => new TrendingCity(
              g.Key.City!,
              g.Sum(tg => tg.Predictions!.Count(p => p.Active))
          ))
          .OrderByDescending(g => g.NumberOfPredictions)
          .Take(resultCount)
          .ToList();

      Response.AppendCorsHeaders();

      await context.SaveLogEntry("GetTrendingCities", "Completed", "Information", executionInstanceId);
      return Ok(trendingCities);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("GetTrendingCities", ex, executionInstanceId);
      return StatusCode(500);
    }
  }

  [HttpGet("Markets/Trending", Name = "GetTrendingMarkets")]
  [SwaggerOperation("Get trending markets")]
  public async Task<ActionResult<List<TrendingMarket>>> GetTrendingMarkets()
  {
    var executionInstanceId = Guid.NewGuid().ToString();

    try
    {
      if (!HttpContext.CheckRateLimit(cache, config, "GetTrendingMarkets", out var offendingIpAddress))
      {
        await context.SaveLogEntry("GetTrendingMarkets", $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
        return StatusCode(429);
      }

      await context.SaveLogEntry("GetTrendingMarkets", "Started", "Information", executionInstanceId);
      const int resultCount = 200;

      var properties = await context.Property
          .Where(tg => tg.Predictions != null)
          .Include(tg => tg.Predictions)
          .ToListAsync();

      var trendingMarkets = properties
          .GroupBy(tg => new { tg.Market })
          .Select(g => new TrendingMarket(
              g.Key.Market,
              g.Sum(tg => tg.Predictions!.Count(p => p.Active))
          ))
          .OrderByDescending(g => g.NumberOfPredictions)
          .Take(resultCount)
          .ToList();

      Response.AppendCorsHeaders();

      await context.SaveLogEntry("GetTrendingMarkets", "Completed", "Information", executionInstanceId);
      return Ok(trendingMarkets);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("GetTrendingMarkets", ex, executionInstanceId);
      return StatusCode(500);
    }
  }

  private static IQueryable<PropertyContext> ApplyMinPredictionsFilter(IQueryable<PropertyContext> query, PropertyFilter filter)
  {
    if (filter.MinPredictions.HasValue)
    {
      var minPredictions = Math.Clamp(filter.MinPredictions.Value, 0, 50);

      query = query.Where(tg =>
          tg.Predictions!.Count(p => p.Active) >= minPredictions);
    }

    return query;
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
      "predictions_asc" => query.OrderBy(tg => tg.Predictions!.Count(p => p.Active)),
      "predictions_desc" => query.OrderByDescending(tg => tg.Predictions!.Count(p => p.Active)),
      _ => query.OrderByDescending(tg => tg.ListingDate)
    };
  }

  private static async Task<List<Property>> GetProperties(IQueryable<PropertyContext> query, PropertyFilter filter)
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

      var propertySalePrice = data.SalePrice ?? 0;


      var e = data.EstimateStats!.MaxBy(e => e.CreateDate);
      if (e != null)
      {
        property.EstimateStats = new Models.PropertyEstimateStats(e.FirstEstimateDate, e.LastEstimateDate, e.NumEstimates, e.AvgEstimate,
            e.MinEstimate, e.MaxEstimate, e.ClosestEstimate, e.FirstEstimateAmount, e.LastEstimateAmount);
      }

      if (data.Predictions != null)
      {
        var predictions = new List<Tuple<PropertyPrediction, double, double>>();
        foreach (var prediction in data.Predictions.Where(p => p.Active))
        {
          var tgp = new PropertyPrediction(prediction.Miner.HotKey,
              prediction.Miner.ColdKey, prediction.PredictionDate, prediction.PredictedSalePrice,
              prediction.PredictedSaleDate);

          var incentive = prediction.Miner.Incentive;
          var priceDiff = Math.Abs(propertySalePrice - tgp.PredictedSalePrice);

          predictions.Add(new Tuple<PropertyPrediction, double, double>(tgp, incentive, priceDiff));
        }

        List<PropertyPrediction> returnedPredictions;

        if (filter.RankMethod == "Incentive")
        {
          returnedPredictions = predictions.OrderByDescending(p => p.Item2).Take(filter.TopPredictionCount).Select(p => p.Item1).ToList();
        }
        else // if (filter.RankMethod == "PredictedSalePrice")
        {
          returnedPredictions = predictions.OrderBy(p => p.Item3).Take(filter.TopPredictionCount).Select(p => p.Item1).ToList();
        }

        property.Predictions.AddRange(returnedPredictions);
      }

      properties.Add(property);
    }

    return properties;
  }
}