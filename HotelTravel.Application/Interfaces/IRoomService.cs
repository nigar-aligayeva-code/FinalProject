using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.DTOs.Rooms;

namespace HotelTravel.Application.Interfaces;

public interface IRoomService
{
    Task<List<GetRoomDto>> GetAllAsync();

    Task<GetRoomDto?> GetByIdAsync(int id);

    Task CreateAsync(CreateRoomDto dto);

    Task<bool> UpdateAsync(int id, UpdateRoomDto dto);

    Task<bool> DeleteAsync(int id);

    // Silməzdən əvvəl: aktiv / gələcək rezervasiya sayı
    Task<int> CountUpcomingBookingsAsync(int roomId);

    // Ana səhifə - ən yüksək reytinqli otaqlar
    Task<List<FeaturedRoomDto>> GetFeaturedRoomsAsync(int count);


    // =========================
    // ROOM GALLERY
    // =========================

    Task<GetRoomImageDto?> GetImageByIdAsync(int imageId);

    Task<bool> DeleteImageAsync(int imageId);

    // =========================
    // MAIN IMAGE
    // =========================
    Task<bool> DeleteMainImageAsync(int roomId);
    // =========================
    // SEARCH
    // =========================

    Task<List<GetRoomDto>> SearchAvailableRoomsAsync(
        RoomSearchDto search);


    // =========================
    // CALENDAR
    // =========================

    // Bütün otaqların dolu olduğu günləri qaytarır
    Task<List<DateTime>> GetFullyBookedDatesAsync(
        DateTime startDate,
        DateTime endDate);
}