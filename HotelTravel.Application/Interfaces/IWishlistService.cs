using HotelTravel.Application.DTOs.Home;

namespace HotelTravel.Application.Interfaces;

public interface IWishlistService
{
    // Saxla / sil (ürək düyməsi). Saved = indi saxlanılıbmı
    Task<(bool Success, bool Saved)> ToggleAsync(string appUserId, int roomId);

    // Səhifədə hansı ürəklərin dolu olacağını bilmək üçün
    Task<HashSet<int>> GetSavedRoomIdsAsync(string? appUserId);

    // "Saved" səhifəsi
    Task<List<FeaturedRoomDto>> GetSavedRoomsAsync(string appUserId);
}
