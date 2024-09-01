namespace Nextplace.Api;

public static class HelperExtensions
{
    public static void AppendCorsHeaders(this HttpResponse response)
    {
        response.Headers.Append("access-control-allow-origin", "*");
        response.Headers.Append("access-control-expose-headers", "*");
    }
}