using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using HotelTravel.Application.DTOs.Weather;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HotelTravel.Infrastructure.Services;

// Open-Meteo (pulsuz, API açarı yoxdur). Nəticə 30 dəqiqə yaddaşda saxlanılır.
public class OpenMeteoWeatherService : IWeatherService
{
    private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";
    private const string ArchiveUrl = "https://archive-api.open-meteo.com/v1/archive";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly HttpClient _http;
    private readonly IAppDbContext _context;
    private readonly IMemoryCache _cache;

    public OpenMeteoWeatherService(HttpClient http, IAppDbContext context, IMemoryCache cache)
    {
        _http = http;
        _context = context;
        _cache = cache;
    }

    public async Task<TripWeatherDto?> GetForHotelAsync(int hotelId, DateTime? from, DateTime? to)
    {
        var hotel = await _context.Hotels
            .Where(h => h.Id == hotelId && !h.IsDeleted)
            .Select(h => new { h.City, h.Latitude, h.Longitude })
            .FirstOrDefaultAsync();

        if (hotel == null || (hotel.Latitude == 0 && hotel.Longitude == 0))
            return null;

        return await GetAsync(hotel.City, hotel.Latitude, hotel.Longitude, from, to);
    }

    public async Task<TripWeatherDto?> GetForCityAsync(string city, DateTime? from, DateTime? to)
    {
        if (string.IsNullOrWhiteSpace(city))
            return null;

        var name = city.Trim();

        // Şəhərin koordinatı = oradakı otellərin ortalaması
        var points = await _context.Hotels
            .Where(h => !h.IsDeleted && h.City == name && (h.Latitude != 0 || h.Longitude != 0))
            .Select(h => new { h.Latitude, h.Longitude })
            .ToListAsync();

        if (points.Count == 0)
            return null;

        return await GetAsync(name, points.Average(p => p.Latitude), points.Average(p => p.Longitude), from, to);
    }

    private async Task<TripWeatherDto?> GetAsync(string location, double lat, double lng, DateTime? from, DateTime? to)
    {
        var window = WeatherRules.GetWindow(from, to, DateTime.Today);

        if (window == null)
            return null;

        var cacheKey = $"weather:{lat:F2}:{lng:F2}:{window.Start:yyyyMMdd}:{window.End:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out TripWeatherDto? cached))
            return cached;

        var url =
            $"{(window.IsForecast ? ForecastUrl : ArchiveUrl)}" +
            $"?latitude={lat.ToString("F4", Inv)}&longitude={lng.ToString("F4", Inv)}" +
            $"&start_date={window.Start:yyyy-MM-dd}&end_date={window.End:yyyy-MM-dd}" +
            "&timezone=auto&daily=weather_code,temperature_2m_max,temperature_2m_min," +
            (window.IsForecast ? "precipitation_probability_max" : "precipitation_sum");

        OpenMeteoResponse? response;

        try
        {
            response = await _http.GetFromJsonAsync<OpenMeteoResponse>(url);
        }
        catch (Exception)
        {
            // İnternet yoxdursa hava sadəcə göstərilmir
            return null;
        }

        var daily = response?.Daily;

        if (daily?.Time == null || daily.Time.Count == 0)
            return null;

        var days = new List<WeatherDayDto>();

        for (var i = 0; i < daily.Time.Count; i++)
        {
            var max = At(daily.TempMax, i);
            var min = At(daily.TempMin, i);

            if (max == null || min == null)
                continue;

            var code = (int)(At(daily.WeatherCode, i) ?? 0);
            var (summary, icon) = WeatherRules.Describe(code);

            days.Add(new WeatherDayDto
            {
                Date = DateTime.Parse(daily.Time[i], Inv).AddYears(window.YearShift),
                Code = code,
                Summary = summary,
                Icon = icon,
                TempMax = WeatherRules.Round(max.Value),
                TempMin = WeatherRules.Round(min.Value),
                RainChance = window.IsForecast && At(daily.RainChance, i) is { } chance ? (int)chance : null,
                RainMm = !window.IsForecast && At(daily.RainSum, i) is { } mm ? Math.Round(mm, 1) : null
            });
        }

        if (days.Count == 0)
            return null;

        var result = new TripWeatherDto
        {
            Location = location,
            IsForecast = window.IsForecast,
            Days = days,
            AverageMax = WeatherRules.Round(days.Average(d => d.TempMax)),
            AverageMin = WeatherRules.Round(days.Average(d => d.TempMin)),
            Advice = WeatherRules.GetAdvice(days)
        };

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

        return result;
    }

    private static double? At(List<double?>? values, int index)
        => values != null && index < values.Count ? values[index] : null;

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("daily")]
        public OpenMeteoDaily? Daily { get; set; }
    }

    private sealed class OpenMeteoDaily
    {
        [JsonPropertyName("time")]
        public List<string>? Time { get; set; }

        [JsonPropertyName("weather_code")]
        public List<double?>? WeatherCode { get; set; }

        [JsonPropertyName("temperature_2m_max")]
        public List<double?>? TempMax { get; set; }

        [JsonPropertyName("temperature_2m_min")]
        public List<double?>? TempMin { get; set; }

        [JsonPropertyName("precipitation_probability_max")]
        public List<double?>? RainChance { get; set; }

        [JsonPropertyName("precipitation_sum")]
        public List<double?>? RainSum { get; set; }
    }
}
