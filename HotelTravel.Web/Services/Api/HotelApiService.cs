using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.DTOs.Hotels;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// IHotelService -> API (api/hotels)
public class HotelApiService : IHotelService
{
    private readonly ApiClient _api;

    public HotelApiService(ApiClient api)
    {
        _api = api;
    }

    public Task<List<GetHotelDto>> GetAllAsync()
        => _api.GetAsync<List<GetHotelDto>>("api/hotels");

    public Task<GetHotelDto?> GetByIdAsync(int id)
        => _api.GetOrNullAsync<GetHotelDto>($"api/hotels/{id}");

    public async Task CreateAsync(CreateHotelDto dto)
        => ApiClient.EnsureOk(await _api.PostAsync("api/hotels", dto));

    public async Task<bool> UpdateAsync(int id, UpdateHotelDto dto)
        => (await _api.PutAsync($"api/hotels/{id}", dto)).Success;

    public async Task<bool> DeleteAsync(int id)
        => (await _api.DeleteAsync($"api/hotels/{id}")).Success;

    public Task<int> CountUpcomingBookingsAsync(int hotelId)
        => _api.GetAsync<int>($"api/hotels/{hotelId}/upcoming-bookings");

    public Task<List<DestinationDto>> GetPopularDestinationsAsync(int count)
        => _api.GetAsync<List<DestinationDto>>(ApiUrl.For("api/hotels/destinations").Add("count", count));
}
