using HotelTravel.Application.DTOs.Taxis;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Interfaces;

public interface ITaxiService
{
    // Saytda
    Task<List<TaxiHotelDto>> GetHotelsAsync();

    Task<TaxiHotelDto?> GetHotelAsync(int hotelId);

    List<TaxiOptionDto> GetOptions(int distanceKm, int passengers, int luggage);

    Task<(bool Success, string Message, string? ConfirmationCode)>
        BookAsync(CreateTaxiBookingDto dto, string? appUserId);

    Task<TaxiBookingDto?> GetByCodeAsync(string confirmationCode);

    Task<List<TaxiBookingDto>> GetByUserAsync(string appUserId);

    Task<(bool Success, string Message)> CancelByUserAsync(int taxiBookingId, string appUserId);

    // Admin
    Task<List<TaxiBookingDto>> GetAllAsync(TaxiBookingStatus? status);

    Task<bool> SetStatusAsync(int id, TaxiBookingStatus status);
}
