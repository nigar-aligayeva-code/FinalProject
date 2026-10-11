
using HotelTravel.Application.DTOs.Bookings;

namespace HotelTravel.Application.Interfaces;

public interface IBookingCreationService
{
    Task<(bool Success, string Message, string? ConfirmationCode)>
        CreateAsync(CreateBookingDto dto);
}
