using System.Globalization;
using System.Text.Json;
using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HotelTravel.Infrastructure.Services;

/// <summary>
/// AviationStack (https://aviationstack.com) — real reys məlumatı.
/// GET /flights?access_key=...&dep_iata=GYD&limit=100 → bu günkü çıxışlar.
/// Qeyd: AviationStack "scheduled" vaxtını yerli saatla, amma "+00:00" yazır — saatı olduğu kimi götürürük.
/// Codeshare (başqa şirkətin satdığı eyni reys) təkrarları atılır. Nəticə 12 saat yaddaşda saxlanılır.
/// </summary>
public class AviationStackScheduleProvider : IRealFlightScheduleProvider
{
    private readonly HttpClient _http;
    private readonly AviationStackOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AviationStackScheduleProvider> _logger;

    public AviationStackScheduleProvider(
        HttpClient http,
        AviationStackOptions options,
        IMemoryCache cache,
        ILogger<AviationStackScheduleProvider> logger)
    {
        _http = http;
        _options = options;
        _cache = cache;
        _logger = logger;
    }

    public bool IsEnabled => _options.IsEnabled;

    public async Task<RealScheduleResult> GetDeparturesAsync(string fromIata, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return RealScheduleResult.Fail("Real flight data is not configured. Add an AviationStack access key.");

        var key = $"aviationstack:{fromIata.ToUpperInvariant()}:{DateTime.UtcNow:yyyyMMdd}";

        if (_cache.TryGetValue(key, out RealScheduleResult? cached) && cached is not null)
            return cached;

        var url = $"{_options.BaseUrl.TrimEnd('/')}/flights" +
                  $"?access_key={Uri.EscapeDataString(_options.AccessKey!)}" +
                  $"&dep_iata={Uri.EscapeDataString(fromIata)}&limit=100";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var result = Parse(json.RootElement, fromIata);

            if (result.Success)
                _cache.Set(key, result, TimeSpan.FromHours(12));

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning("AviationStack request for {Airport} failed: {Message}", fromIata, ex.Message);
            return RealScheduleResult.Fail("The flight data service is not reachable right now.");
        }
    }

    /// <summary>JSON cavabını oxuyur (testlər üçün public).</summary>
    public static RealScheduleResult Parse(JsonElement root, string fromIata)
    {
        // {"error": {"code": "...", "message": "..."}}
        if (root.TryGetProperty("error", out var error))
        {
            var message = error.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
            return RealScheduleResult.Fail($"AviationStack: {message}");
        }

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return RealScheduleResult.Fail("AviationStack returned no data.");

        var flights = new List<RealFlight>();

        foreach (var item in data.EnumerateArray())
        {
            // Codeshare təkrarını atırıq (əsl reys başqa nömrə ilə artıq siyahıdadır)
            if (item.TryGetProperty("flight", out var flight) &&
                flight.TryGetProperty("codeshared", out var codeshared) &&
                codeshared.ValueKind == JsonValueKind.Object)
                continue;

            var iata = Text(flight, "iata");
            var airline = item.TryGetProperty("airline", out var al) ? Text(al, "name") : null;
            var airlineCode = item.TryGetProperty("airline", out al) ? Text(al, "iata") : null;

            if (!item.TryGetProperty("departure", out var dep) || !item.TryGetProperty("arrival", out var arr))
                continue;

            var toCode = Text(arr, "iata");
            var depTime = LocalTime(Text(dep, "scheduled"));
            var arrTime = LocalTime(Text(arr, "scheduled"));

            if (string.IsNullOrWhiteSpace(iata) || string.IsNullOrWhiteSpace(airline) ||
                string.IsNullOrWhiteSpace(toCode) || depTime is null || arrTime is null)
                continue;

            flights.Add(new RealFlight(
                FormatNumber(iata, airlineCode),
                airline,
                fromIata.ToUpperInvariant(),
                toCode.ToUpperInvariant(),
                depTime.Value,
                arrTime.Value));
        }

        return new RealScheduleResult(true, null, flights);
    }

    // "J28011" + "J2" -> "J2 8011" (bizim formatda)
    public static string FormatNumber(string iata, string? airlineCode)
    {
        iata = iata.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(airlineCode) && iata.StartsWith(airlineCode.ToUpperInvariant()))
            return $"{airlineCode.ToUpperInvariant()} {iata[airlineCode.Length..]}";

        return iata.Length > 2 ? $"{iata[..2]} {iata[2..]}" : iata;
    }

    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    // "2026-10-25T08:05:00+00:00" -> 08:05 (yerli saat kimi)
    private static TimeSpan? LocalTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 16)
            return null;

        return TimeSpan.TryParseExact(value.Substring(11, 5), "hh\\:mm", CultureInfo.InvariantCulture, out var time)
            ? time
            : null;
    }
}
