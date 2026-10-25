using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// IFlightNetworkService -> API (api/flights/routes/generate, import-real)
public class FlightNetworkApiService : IFlightNetworkService
{
    private readonly ApiClient _api;
    public FlightNetworkApiService(ApiClient api)
    {
        _api = api;
    }

    // Admin səhifəsi üçün: "real məlumat" düyməsi aktivdirmi
    public async Task<bool> IsRealDataEnabledAsync()
    {
        try
        {
            return (await _api.GetAsync<NetworkStatus>("api/flights/network")).RealDataEnabled;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Task<FlightNetworkResultDto> GenerateMissingAsync(CancellationToken cancellationToken = default)
        => PostAsync("api/flights/routes/generate");

    public Task<FlightNetworkResultDto> ImportRealAsync(CancellationToken cancellationToken = default)
        => PostAsync("api/flights/routes/import-real");

    private async Task<FlightNetworkResultDto> PostAsync(string url)
    {
        try
        {
            return await _api.PostForAsync<FlightNetworkResultDto>(url, new { });
        }
        catch (ApiException ex)
        {
            return new FlightNetworkResultDto { Success = false, Message = ex.Message };
        }
    }

    private sealed class NetworkStatus
    {
        public bool RealDataEnabled { get; set; }
    }
}
