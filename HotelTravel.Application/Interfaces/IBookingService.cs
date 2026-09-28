using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Interfaces;

public interface IBookingService
{
    Task<List<GetBookingDto>> GetAllAsync();

    Task<GetBookingDto?> GetByIdAsync(int id);

    Task<(bool Success, string Message, string? ConfirmationCode)>
        CreateAsync(CreateBookingDto dto);

    Task<bool> UpdateStatusAsync(
        int id,
        BookingStatus status);

    Task<List<OccupancyCalendarDto>>
        GetOccupancyCalendarAsync();

    // Find My Booking
    Task<GetBookingDto?> GetByConfirmationCodeAsync(
        string confirmationCode);
}