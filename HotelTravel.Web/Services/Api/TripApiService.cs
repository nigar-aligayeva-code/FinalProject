using HotelTravel.Application.DTOs.Trips;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// ITripPackageService -> API (api/trips)
public class TripApiService : ITripPackageService
{
    private readonly ApiClient _api;

    public TripApiService(ApiClient api)
    {
        _api = api;
    }

    public Task<List<string>> GetOriginsAsync()
        => _api.GetAsync<List<string>>("api/trips/origins");

    public Task<List<string>> GetDestinationsAsync()
        => _api.GetAsync<List<string>>("api/trips/destinations");

    public Task<TripPlanDto> PlanAsync(TripSearchDto search)
        => _api.PostForAsync<TripPlanDto>("api/trips/plan", search);

    public async Task<(bool Success, string Message, string? Code)> BookAsync(CreateTripPackageDto dto, string appUserId)
    {
        // İstifadəçi API-də token-dən tanınır
        var result = await _api.PostAsync("api/trips", dto);

        return (result.Success, result.Message, result.Code);
    }

    public Task<TripPackageDto?> GetByCodeAsync(string code, string appUserId)
        => _api.GetOrNullAsync<TripPackageDto>($"api/trips/{ApiUrl.Segment(code)}");

    public Task<List<TripPackageDto>> GetByUserAsync(string appUserId)
        => _api.GetAsync<List<TripPackageDto>>("api/trips/mine");
}
