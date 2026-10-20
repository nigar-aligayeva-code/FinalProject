using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HotelTravel.API.Health;

// /health cavabını oxunaqlı JSON kimi yazır
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var body = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName,
            serverTime = DateTime.Now,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                description = e.Value.Description
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(body, Options));
    }
}
