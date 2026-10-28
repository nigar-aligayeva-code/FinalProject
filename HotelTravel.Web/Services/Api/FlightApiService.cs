using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Web.Services.Api;

// IFlightService -> API (api/flights)
public class FlightApiService : IFlightService
{
    private readonly ApiClient _api;

    public FlightApiService(ApiClient api)
    {
        _api = api;
    }

    // SAYT
    public Task<List<string>> GetCitiesAsync()
        => _api.GetAsync<List<string>>("api/flights/cities");

    public async Task<List<FlightOptionDto>> SearchAsync(
        string fromCity,
        string toCity,
        DateTime date,
        int passengers,
        CabinClass cabin)
    {
        if (string.IsNullOrWhiteSpace(fromCity) || string.IsNullOrWhiteSpace(toCity))
            return new List<FlightOptionDto>();

        return await _api.GetAsync<List<FlightOptionDto>>(
            ApiUrl.For("api/flights/search")
                .Add("fromCity", fromCity)
                .Add("toCity", toCity)
                .Add("date", date)
                .Add("passengers", passengers)
                .Add("cabin", cabin));
    }

    public async Task<List<FlightDayFareDto>> GetDayFaresAsync(
        string fromCity,
        string toCity,
        DateTime centerDate,
        CabinClass cabin)
    {
        if (string.IsNullOrWhiteSpace(fromCity) || string.IsNullOrWhiteSpace(toCity))
            return new List<FlightDayFareDto>();

        return await _api.GetAsync<List<FlightDayFareDto>>(
            ApiUrl.For("api/flights/day-fares")
                .Add("fromCity", fromCity)
                .Add("toCity", toCity)
                .Add("date", centerDate)
                .Add("cabin", cabin));
    }

    public Task<FlightOptionDto?> GetOptionAsync(int routeId, DateTime date, int passengers, CabinClass cabin)
        => _api.GetOrNullAsync<FlightOptionDto>(
            ApiUrl.For($"api/flights/options/{routeId}")
                .Add("date", date)
                .Add("passengers", passengers)
                .Add("cabin", cabin));

    public async Task<(bool Success, string Message, string? ConfirmationCode)>
        BookAsync(CreateFlightBookingDto dto, string? appUserId)
    {
        var result = await _api.PostAsync("api/flights/bookings", dto);
        return (result.Success, result.Message, result.Code);
    }

    public async Task<FlightBookingDto?> GetByCodeAsync(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return null;

        return await _api.GetOrNullAsync<FlightBookingDto>(
            $"api/flights/bookings/by-code/{ApiUrl.Segment(confirmationCode)}");
    }

    public async Task<List<FlightBookingDto>> GetByUserAsync(string appUserId)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return new List<FlightBookingDto>();

        return await _api.GetAsync<List<FlightBookingDto>>("api/flights/bookings/mine");
    }

    public async Task<(bool Success, string Message)> CancelByUserAsync(int flightBookingId, string appUserId)
    {
        var result = await _api.PostAsync($"api/flights/bookings/{flightBookingId}/cancel-mine");
        return (result.Success, result.Message);
    }

    // ADMIN
    public Task<List<FlightRouteDto>> GetRoutesAsync(string? city)
        => _api.GetAsync<List<FlightRouteDto>>(ApiUrl.For("api/flights/routes").Add("city", city));

    public Task<List<FlightBookingDto>> GetBookingsAsync(FlightBookingStatus? status)
        => _api.GetAsync<List<FlightBookingDto>>(ApiUrl.For("api/flights/bookings").Add("status", status));

    public async Task<bool> SetBookingStatusAsync(int id, FlightBookingStatus status)
        => (await _api.PutAsync(
            $"api/flights/bookings/{id}/status",
            new StatusUpdateDto<FlightBookingStatus> { Status = status })).Success;
}
