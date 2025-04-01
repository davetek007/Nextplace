using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Nextplace.Api.Db;

namespace Nextplace.Api.Helpers;

public class ApiFilter(AppDbContext dbContext, IConfiguration configuration, IMemoryCache cache) : IAsyncActionFilter
{
  public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
  {
    var httpContext = context.HttpContext;

    var executionInstanceId = Guid.NewGuid().ToString();
    httpContext.Items["executionInstanceId"] = executionInstanceId;

    var methodName = ExtractMethodName(context.ActionDescriptor.DisplayName!);
    var response = httpContext.Response;

    if (!methodName.Equals("PostPredictions", StringComparison.InvariantCultureIgnoreCase))
    {
      await dbContext.SaveLogEntry(methodName, "Started", "Information", executionInstanceId);
    }

    if (!httpContext.CheckRateLimit(cache, configuration, methodName, out var offendingIpAddress))
    {
      await dbContext.SaveLogEntry(methodName, $"Rate limit exceeded by {offendingIpAddress}", "Warning", executionInstanceId);
      context.Result = new StatusCodeResult(429);
      await dbContext.SaveChangesAsync();
      return;
    }

    ActionExecutedContext? executedContext;
    try
    {
      executedContext = await next();
    }
    catch (Exception ex)
    {
      await dbContext.SaveLogEntry(methodName, ex, executionInstanceId);
      context.Result = new StatusCodeResult(500);
      await dbContext.SaveChangesAsync();
      return;
    }

    if (executedContext is { Exception: not null, ExceptionHandled: false })
    {
      await dbContext.SaveLogEntry(methodName, executedContext.Exception, executionInstanceId);
      executedContext.ExceptionHandled = true;
      context.Result = new StatusCodeResult(500);
    }
    else
    {
      if (!methodName.Equals("PostPredictions", StringComparison.InvariantCultureIgnoreCase))
      {
        await dbContext.SaveLogEntry(methodName, "Completed", "Information", executionInstanceId);
      }
    }

    response.AppendCorsHeaders();
    await dbContext.SaveChangesAsync();
  
  }

  private string ExtractMethodName(string fullDisplayName)
  {
    if (string.IsNullOrWhiteSpace(fullDisplayName))
      return "UnknownMethod";

    // Remove any trailing " (…)" portion.
    int parenIndex = fullDisplayName.IndexOf(" (", StringComparison.Ordinal);
    if (parenIndex > 0)
      fullDisplayName = fullDisplayName.Substring(0, parenIndex);

    // Extract the part after the last period.
    int lastDot = fullDisplayName.LastIndexOf('.');
    if (lastDot >= 0 && lastDot < fullDisplayName.Length - 1)
      return fullDisplayName.Substring(lastDot + 1);

    return fullDisplayName;
  }
}