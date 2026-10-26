using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Web.Services.Api;

// IBookingService -> API (api/bookings)
// İstifadəçi Id-si API-yə göndərilmir: API onu JWT token-dən götürür.
public class BookingApiService : IBookingService
{
    private readonly ApiClient _api;

    public BookingApiService(ApiClient api)
    {
        _api = api;
    }

    public Task<List<GetBookingDto>> GetAllAsync()
        => _api.GetAsync<List<GetBookingDto>>("api/bookings");

    public Task<GetBookingDto?> GetByIdAsync(int id)
        => _api.GetOrNullAsync<GetBookingDto>($"api/bookings/{id}");

    public async Task<(bool Success, string Message, string? ConfirmationCode)>
        CreateAsync(CreateBookingDto dto, string? appUserId = null)
    {
        var result = await _api.PostAsync("api/bookings", dto);
        return (result.Success, result.Message, result.Code);
    }

    public async Task<bool> UpdateStatusAsync(int id, BookingStatus status)
        => (await _api.PutAsync(
            $"api/bookings/{id}/status",
            new StatusUpdateDto<BookingStatus> { Status = status })).Success;

    public async Task<(bool Success, string Message)> ConfirmRequestAsync(int id)
    {
        var result = await _api.PostAsync($"api/bookings/{id}/confirm");
        return (result.Success, result.Message);
    }

    public async Task<(bool Success, string Message)> RejectRequestAsync(int id)
    {
        var result = await _api.PostAsync($"api/bookings/{id}/reject");
        return (result.Success, result.Message);
    }

    public Task<int> CountPendingAsync()
        => _api.GetAsync<int>("api/bookings/pending-count");

    public Task<List<OccupancyCalendarDto>> GetOccupancyCalendarAsync()
        => _api.GetAsync<List<OccupancyCalendarDto>>("api/bookings/occupancy-calendar");

    public async Task<GetBookingDto?> GetByConfirmationCodeAsync(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return null;

        return await _api.GetOrNullAsync<GetBookingDto>(
            $"api/bookings/by-code/{ApiUrl.Segment(confirmationCode)}");
    }

    public async Task<(bool Success, string Message)> CancelByGuestAsync(
        string confirmationCode,
        string email)
    {
        var result = await _api.PostAsync("api/bookings/cancel", new CancelBookingDto
        {
            ConfirmationCode = confirmationCode ?? string.Empty,
            Email = email ?? string.Empty
        });

        return (result.Success, result.Message);
    }

    public async Task<List<GetBookingDto>> GetByUserIdAsync(string appUserId)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return new List<GetBookingDto>();

        return await _api.GetAsync<List<GetBookingDto>>("api/bookings/mine");
    }

    public async Task<(bool Success, string Message)> CancelByUserAsync(int bookingId, string appUserId)
    {
        var result = await _api.PostAsync($"api/bookings/{bookingId}/cancel-mine");
        return (result.Success, result.Message);
    }

    public Task<PriceQuoteDto> GetPriceQuoteAsync(
        int roomId,
        DateTime checkInDate,
        DateTime checkOutDate,
        string? email,
        string? appUserId,
        string? promoCode)
        => _api.GetAsync<PriceQuoteDto>(
            ApiUrl.For("api/bookings/price-quote")
                .Add("roomId", roomId)
                .Add("checkInDate", checkInDate)
                .Add("checkOutDate", checkOutDate)
                .Add("email", email)
                .Add("promoCode", promoCode));

    public async Task<(bool Success, string Message)> ModifyAsync(
        ModifyBookingDto dto,
        string? appUserId = null)
    {
        var result = await _api.PostAsync("api/bookings/modify", dto);
        return (result.Success, result.Message);
    }
}
