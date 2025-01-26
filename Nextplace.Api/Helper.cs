using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace Nextplace.Api;

public static class HelperExtensions
{
  public static void AppendCorsHeaders(this HttpResponse response)
  {
    response.Headers.Append("access-control-allow-origin", "*");
    response.Headers.Append("access-control-expose-headers", "*");
  }

  public static bool IsIpWhitelisted(IConfiguration configuration, List<string> ipAddresses, out bool whitelistOnly)
  {
    var whitelist = configuration.GetSection("IpWhitelist").Get<string[]>();

    whitelistOnly = configuration.GetSection("IpWhitelistOnly").Get<bool>();
    return whitelist != null && ipAddresses.Any(ip => whitelist.Any(whitelistedIp =>
      string.Equals(whitelistedIp, ip, StringComparison.OrdinalIgnoreCase)));
  }

  public static List<string> GetIpAddressesFromHeader(this HttpContext context, out string logString)
  {
    var ipAddressList = new List<string>();
    var ipAddressLog = new StringBuilder();
    var clientIps = context.Request.Headers["X-Azure-ClientIP"];
    var socketIps = context.Request.Headers["X-Azure-SocketIP"];
    var forwardedForIps = context.Request.Headers["X-Forwarded-For"];
    var remoteIp = context.Connection.RemoteIpAddress?.ToString();

    if (clientIps.Count != 0)
    {
      var ipAddresses = clientIps.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
      ipAddressList.AddRange(ipAddresses!);
      ipAddressLog.Append($"X-Azure-ClientIP: {string.Join(',', ipAddresses)}");
    }

    if (socketIps.Count != 0)
    {
      var ipAddresses = socketIps.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
      ipAddressList.AddRange(ipAddresses!);
      if (ipAddressLog.Length != 0) ipAddressLog.Append(", ");
      ipAddressLog.Append($"X-Azure-SocketIP: {string.Join(',', ipAddresses)}");
    }

    if (forwardedForIps.Count != 0)
    {
      var ipAddresses = forwardedForIps.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
      ipAddressList.AddRange(ipAddresses!);
      if (ipAddressLog.Length != 0) ipAddressLog.Append(", ");
      ipAddressLog.Append($"X-Forwarded-For: {string.Join(',', ipAddresses)}");
    }

    if (remoteIp != null)
    {
      ipAddressList.Add(remoteIp);
      if (ipAddressLog.Length != 0) ipAddressLog.Append(", ");
      ipAddressLog.Append($"Remote IP: {remoteIp}");
    }

    logString = ipAddressLog.ToString();

    return ipAddressList;
  }


  public static List<string> GetIpAddressesFromHeader(this HttpContext context, out string logString, out string? clientIp)
  {
    clientIp = null;
    var ipAddressList = new List<string>();
    var ipAddressLog = new StringBuilder();
    var clientIps = context.Request.Headers["X-Azure-ClientIP"];
    var socketIps = context.Request.Headers["X-Azure-SocketIP"];
    var forwardedForIps = context.Request.Headers["X-Forwarded-For"];
    var remoteIp = context.Connection.RemoteIpAddress?.ToString();

    if (clientIps.Count != 0)
    {
      var ipAddresses = clientIps.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
      ipAddressList.AddRange(ipAddresses!);
      ipAddressLog.Append($"X-Azure-ClientIP: {string.Join(',', ipAddresses)}");
      clientIp = ipAddressList.First();
    }

    if (socketIps.Count != 0)
    {
      var ipAddresses = socketIps.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
      ipAddressList.AddRange(ipAddresses!);
      if (ipAddressLog.Length != 0) ipAddressLog.Append(", ");
      ipAddressLog.Append($"X-Azure-SocketIP: {string.Join(',', ipAddresses)}");
    }

    if (forwardedForIps.Count != 0)
    {
      var ipAddresses = forwardedForIps.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
      ipAddressList.AddRange(ipAddresses!);
      if (ipAddressLog.Length != 0) ipAddressLog.Append(", ");
      ipAddressLog.Append($"X-Forwarded-For: {string.Join(',', ipAddresses)}");
    }

    if (remoteIp != null)
    {
      ipAddressList.Add(remoteIp);
      if (ipAddressLog.Length != 0) ipAddressLog.Append(", ");
      ipAddressLog.Append($"Remote IP: {remoteIp}");
      if (clientIp == null) clientIp = remoteIp;
    }

    logString = ipAddressLog.ToString();

    return ipAddressList;
  }
  public static bool CheckRateLimit(this HttpContext context, IMemoryCache cache, IConfiguration config, string methodName, out string? offendingIpAddress)
  {
    offendingIpAddress = null;
    var ipAddressList = new List<string?>();
    var clientIps = context.Request.Headers["X-Azure-ClientIP"];
    var forwardedForIps = context.Request.Headers["X-Forwarded-For"];


    if (clientIps.Count != 0)
    {
      var ipAddresses = clientIps.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
      ipAddressList.AddRange(ipAddresses!);
    }

    if (forwardedForIps.Count != 0)
    {
      var ipAddresses = forwardedForIps.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
      ipAddressList.AddRange(ipAddresses!);
    }

    if (!ipAddressList.Any())
    {
      var fallbackIp = context.Connection.RemoteIpAddress?.ToString();
      if (!string.IsNullOrEmpty(fallbackIp))
      {
        ipAddressList.Add(fallbackIp);
      }
    }

    var maxRequestsPerSecond = config.GetValue<int>("MaxRequestsPerSecond");

    foreach (var ipAddress in ipAddressList)
    {
      var cacheKey = $"{ipAddress}_{methodName}";

      if (cache.TryGetValue(cacheKey, out RateLimitInfo rateLimitInfo))
      {
        var currentTime = DateTime.UtcNow;

        if ((currentTime - rateLimitInfo.Timestamp).TotalSeconds >= 1)
        {
          rateLimitInfo.Timestamp = currentTime;
          rateLimitInfo.RequestCount = 1;
        }
        else
        {
          rateLimitInfo.RequestCount++;

          if (rateLimitInfo.RequestCount > maxRequestsPerSecond)
          {
            offendingIpAddress = ipAddress;
            return false;
          }
        }

        cache.Set(cacheKey, rateLimitInfo, TimeSpan.FromSeconds(1));
      }
      else
      {
        var newRateLimitInfo = new RateLimitInfo
        {
          Timestamp = DateTime.UtcNow,
          RequestCount = 1
        };
        cache.Set(cacheKey, newRateLimitInfo, TimeSpan.FromSeconds(1));
      }
    }

    return true;

  }
  public class RateLimitInfo
  {
    public DateTime Timestamp { get; set; }
    public int RequestCount { get; set; }
  }
}