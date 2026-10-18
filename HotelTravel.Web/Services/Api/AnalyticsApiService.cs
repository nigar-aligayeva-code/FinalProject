using HotelTravel.Application.DTOs.Analytics;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// IAnalyticsService -> API (api/analytics)
public class AnalyticsApiService : IAnalyticsService
{
    private readonly ApiClient _api;

    public AnalyticsApiService(ApiClient api)
    {
        _api = api;
    }

    public Task<AnalyticsDto> GetAsync()
        => _api.GetAsync<AnalyticsDto>("api/analytics");
}
