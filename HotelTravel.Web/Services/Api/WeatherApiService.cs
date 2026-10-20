using HotelTravel.Application.DTOs.Weather;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// IWeatherService -> API (api/weather). Hava əlavə məlumatdır:
// alınmasa səhifə xətasız açılır, sadəcə widget görünmür.
public class WeatherApiService : IWeatherService
{
    private readonly ApiClient _api;

    public WeatherApiService(ApiClient api)
    {
        _api = api;
    }

    public Task<TripWeatherDto?> GetForHotelAsync(int hotelId, DateTime? from, DateTime? to)
        => SafeGetAsync(ApiUrl.For($"api/weather/hotel/{hotelId}")
            .Add("from", from?.ToString("yyyy-MM-dd"))
            .Add("to", to?.ToString("yyyy-MM-dd")));

    public Task<TripWeatherDto?> GetForCityAsync(string city, DateTime? from, DateTime? to)
        => SafeGetAsync(ApiUrl.For($"api/weather/city/{ApiUrl.Segment(city)}")
            .Add("from", from?.ToString("yyyy-MM-dd"))
            .Add("to", to?.ToString("yyyy-MM-dd")));

    private async Task<TripWeatherDto?> SafeGetAsync(string url)
    {
        try
        {
            return await _api.GetOrNullAsync<TripWeatherDto>(url);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
