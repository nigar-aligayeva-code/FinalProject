using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Interfaces;

public interface IBookingService
{
    Task<List<GetBookingDto>> GetAllAsync();

    Task<GetBookingDto?> GetByIdAsync(int id);

    Task<(bool Success, string Message, string? ConfirmationCode)>
        CreateAsync(CreateBookingDto dto, string? appUserId = null);

    Task<bool> UpdateStatusAsync(
        int id,
        BookingStatus status);

    Task<List<OccupancyCalendarDto>>
        GetOccupancyCalendarAsync();

    // Find My Booking
    Task<GetBookingDto?> GetByConfirmationCodeAsync(
     string confirmationCode);

    Task<(bool Success, string Message)> CancelByGuestAsync(
        string confirmationCode,
        string email);

    // F4 - Guest dashboard
    Task<List<GetBookingDto>> GetByUserIdAsync(string appUserId);

    Task<(bool Success, string Message)> CancelByUserAsync(
        int bookingId,
        string appUserId);

    Task<(bool Success, string Message)> ModifyAsync(
        ModifyBookingDto dto,
        string? appUserId = null);
}