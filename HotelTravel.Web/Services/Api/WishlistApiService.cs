using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// IWishlistService -> API (api/wishlist). İstifadəçi token-dən müəyyən olunur.
public class WishlistApiService : IWishlistService
{
    private readonly ApiClient _api;

    public WishlistApiService(ApiClient api)
    {
        _api = api;
    }

    public async Task<(bool Success, bool Saved)> ToggleAsync(string appUserId, int roomId)
    {
        var result = await _api.PostAsync($"api/wishlist/toggle/{roomId}");
        return (result.Success, result.Flag ?? false);
    }

    public async Task<HashSet<int>> GetSavedRoomIdsAsync(string? appUserId)
    {
        // Qonaq üçün API-yə müraciət etmirik
        if (string.IsNullOrWhiteSpace(appUserId))
            return new HashSet<int>();

        try
        {
            return new HashSet<int>(await _api.GetAsync<List<int>>("api/wishlist/ids"));
        }
        catch (ApiUnauthorizedException)
        {
            // View içində çağırılır: token köhnəlibsə ürəklər sadəcə boş görünür
            return new HashSet<int>();
        }
    }

    public async Task<List<FeaturedRoomDto>> GetSavedRoomsAsync(string appUserId)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return new List<FeaturedRoomDto>();

        return await _api.GetAsync<List<FeaturedRoomDto>>("api/wishlist");
    }
}
