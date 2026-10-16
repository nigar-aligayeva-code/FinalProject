using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Interfaces;

public interface IFlightService
{
    // Saytda
    Task<List<string>> GetCitiesAsync();

    Task<List<FlightOptionDto>> SearchAsync(
        string fromCity,
        string toCity,
        DateTime date,
        int passengers,
        CabinClass cabin);

    Task<List<FlightDayFareDto>> GetDayFaresAsync(
        string fromCity,
        string toCity,
        DateTime centerDate,
        CabinClass cabin);

    Task<FlightOptionDto?> GetOptionAsync(
        int routeId,
        DateTime date,
        int passengers,
        CabinClass cabin);

    Task<(bool Success, string Message, string? ConfirmationCode)>
        BookAsync(CreateFlightBookingDto dto, string? appUserId);

    Task<FlightBookingDto?> GetByCodeAsync(string confirmationCode);

    Task<List<FlightBookingDto>> GetByUserAsync(string appUserId);

    Task<(bool Success, string Message)> CancelByUserAsync(int flightBookingId, string appUserId);

    // Admin
    Task<List<FlightRouteDto>> GetRoutesAsync(string? city);
    Task<SaveFlightRouteDto?> GetRouteForEditAsync(int id);
    Task<(bool Success, string Message)> SaveRouteAsync(SaveFlightRouteDto dto);
    Task<bool> ToggleRouteAsync(int id);
    Task<List<FlightBookingDto>> GetBookingsAsync(FlightBookingStatus? status);
    Task<bool> SetBookingStatusAsync(int id, FlightBookingStatus status);
}
