using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// IRoomService -> API (api/rooms)
public class RoomApiService : IRoomService
{
    private readonly ApiClient _api;

    public RoomApiService(ApiClient api)
    {
        _api = api;
    }

    public Task<List<GetRoomDto>> GetAllAsync()
        => _api.GetAsync<List<GetRoomDto>>("api/rooms");

    public Task<GetRoomDto?> GetByIdAsync(int id)
        => _api.GetOrNullAsync<GetRoomDto>($"api/rooms/{id}");

    public async Task CreateAsync(CreateRoomDto dto)
        => ApiClient.EnsureOk(await _api.PostAsync("api/rooms", dto));

    public async Task<bool> UpdateAsync(int id, UpdateRoomDto dto)
        => (await _api.PutAsync($"api/rooms/{id}", dto)).Success;

    public async Task<bool> DeleteAsync(int id)
        => (await _api.DeleteAsync($"api/rooms/{id}")).Success;

    public Task<int> CountUpcomingBookingsAsync(int roomId)
        => _api.GetAsync<int>($"api/rooms/{roomId}/upcoming-bookings");

    public Task<List<FeaturedRoomDto>> GetFeaturedRoomsAsync(int count)
        => _api.GetAsync<List<FeaturedRoomDto>>(ApiUrl.For("api/rooms/featured").Add("count", count));

    public Task<GetRoomImageDto?> GetImageByIdAsync(int imageId)
        => _api.GetOrNullAsync<GetRoomImageDto>($"api/rooms/images/{imageId}");

    public async Task<bool> DeleteImageAsync(int imageId)
        => (await _api.DeleteAsync($"api/rooms/images/{imageId}")).Success;

    public async Task<bool> DeleteMainImageAsync(int roomId)
        => (await _api.DeleteAsync($"api/rooms/{roomId}/main-image")).Success;

    public Task<List<GetRoomDto>> SearchAvailableRoomsAsync(RoomSearchDto search)
        => _api.PostForAsync<List<GetRoomDto>>("api/rooms/search", search);

    public Task<List<DateTime>> GetFullyBookedDatesAsync(DateTime startDate, DateTime endDate)
        => _api.GetAsync<List<DateTime>>(
            ApiUrl.For("api/rooms/fully-booked-dates")
                .Add("startDate", startDate)
                .Add("endDate", endDate));
}
