using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CharityCrm.Api.Setup;

/// <summary>Writes the health report as JSON with one entry per check (NFR-OPS-03).</summary>
public static class HealthCheckResponse
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonSerializerOptions.Web));
    }
}
