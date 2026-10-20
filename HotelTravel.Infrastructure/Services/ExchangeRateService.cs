using System.Net.Http.Json;
using System.Text.Json.Serialization;
using HotelTravel.Application.DTOs.Currency;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using Microsoft.Extensions.Caching.Memory;

namespace HotelTravel.Infrastructure.Services;

// Məzənnələr: open.er-api.com (pulsuz, açarsız). 6 saat keşlənir.
// Alınmasa təxmini sabit məzənnələr qaytarılır (sayt heç vaxt xəta vermir).
public class ExchangeRateService : ICurrencyService
{
    private const string Url = "https://open.er-api.com/v6/latest/AZN";
    private const string CacheKey = "currency:rates";

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public ExchangeRateService(HttpClient http, IMemoryCache cache)
    {
        _http = http;
        _cache = cache;
    }

    public async Task<CurrencyRatesDto> GetRatesAsync()
    {
        if (_cache.TryGetValue(CacheKey, out CurrencyRatesDto? cached) && cached != null)
            return cached;

        CurrencyRatesDto result;

        try
        {
            var response = await _http.GetFromJsonAsync<ErApiResponse>(Url);

            if (response?.Result != "success" || response.Rates == null)
                throw new InvalidOperationException("Exchange rate service returned no data.");

            result = new CurrencyRatesDto
            {
                Rates = CurrencyRules.Merge(response.Rates),
                UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(response.TimeLastUpdateUnix).LocalDateTime,
                IsLive = true
            };

            _cache.Set(CacheKey, result, TimeSpan.FromHours(6));
        }
        catch (Exception)
        {
            result = new CurrencyRatesDto
            {
                Rates = CurrencyRules.Merge(null),
                UpdatedAt = DateTime.Now,
                IsLive = false
            };

            // Bir az sonra yenidən cəhd etmək üçün qısa keş
            _cache.Set(CacheKey, result, TimeSpan.FromMinutes(10));
        }

        result.Currencies = CurrencyRules.Supported.ToList();

        return result;
    }

    private sealed class ErApiResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("time_last_update_unix")]
        public long TimeLastUpdateUnix { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}
