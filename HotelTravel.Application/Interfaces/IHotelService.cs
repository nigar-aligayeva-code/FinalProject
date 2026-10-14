using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.DTOs.Hotels;

namespace HotelTravel.Application.Interfaces;

public interface IHotelService
{
    Task<List<GetHotelDto>> GetAllAsync();
    Task<GetHotelDto?> GetByIdAsync(int id);
    Task CreateAsync(CreateHotelDto dto);
    Task<bool> UpdateAsync(int id, UpdateHotelDto dto);
    Task<bool> DeleteAsync(int id);

    // Silməzdən əvvəl: oteldəki aktiv / gələcək rezervasiya sayı
    Task<int> CountUpcomingBookingsAsync(int hotelId);

    // Ana səhifə
    Task<List<DestinationDto>> GetPopularDestinationsAsync(int count);
}