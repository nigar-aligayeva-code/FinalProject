using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Interfaces;

// Otelin real boş yerləri: bağlı tarixlər, qiymətlər, sorğuların yoxlanması.
// Partnyor yalnız öz otelini, admin bütün otelləri idarə edir (yoxlama API-dədir).
public interface IHotelAvailabilityService
{
    Task<HotelAvailabilityDto?> GetAsync(int hotelId);

    Task<List<GetBookingDto>> GetBookingsAsync(int hotelId, BookingStatus? status = null);

    Task<(bool Success, string Message)> AddBlockAsync(int hotelId, CreateRoomBlockDto dto, string? userId = null);

    Task<(bool Success, string Message)> RemoveBlockAsync(int hotelId, int blockId);

    // "Boş yerləri yoxladım, məlumat təzədir"
    Task<bool> MarkUpToDateAsync(int hotelId);

    Task<(bool Success, string Message)> UpdateRoomPriceAsync(int hotelId, int roomId, decimal pricePerNight);

    // Gözləyən sorğunu təsdiqləməzdən əvvəl yoxlama
    Task<AvailabilityCheckDto?> CheckBookingAsync(int bookingId);
}
