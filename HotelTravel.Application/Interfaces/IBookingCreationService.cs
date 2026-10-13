using HotelTravel.Application.DTOs.Bookings;

namespace HotelTravel.Application.Interfaces;

public interface IBookingCreationService
{
    // appUserId: rezervasiyanı daxil olmuş istifadəçi edirsə
    Task<(bool Success, string Message, string? ConfirmationCode)>
        CreateAsync(CreateBookingDto dto, string? appUserId = null);

    // F4 - Rezervasiyanı dəyişmək (tarix və qonaq sayı)
    Task<(bool Success, string Message)>
        ModifyAsync(ModifyBookingDto dto, string? appUserId = null);
}
