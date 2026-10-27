using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Web.Services.Api;

// IHotelAvailabilityService -> API (api/hotels/{id}/availability)
// API özü yoxlayır: admin bütün otelləri, partnyor yalnız öz otelini idarə edə bilər.
public class HotelAvailabilityApiService : IHotelAvailabilityService
{
    private readonly ApiClient _api;

    public HotelAvailabilityApiService(ApiClient api)
    {
        _api = api;
    }

    private static string Base(int hotelId) => $"api/hotels/{hotelId}/availability";

    public Task<HotelAvailabilityDto?> GetAsync(int hotelId)
        => _api.GetOrNullAsync<HotelAvailabilityDto>(Base(hotelId));

    public Task<List<GetBookingDto>> GetBookingsAsync(int hotelId, BookingStatus? status = null)
        => _api.GetAsync<List<GetBookingDto>>(
            ApiUrl.For(Base(hotelId) + "/bookings").Add("status", status?.ToString()));

    public async Task<(bool Success, string Message)> AddBlockAsync(int hotelId, CreateRoomBlockDto dto, string? userId = null)
    {
        var result = await _api.PostAsync(Base(hotelId) + "/blocks", dto);
        return (result.Success, result.Message);
    }

    public async Task<(bool Success, string Message)> RemoveBlockAsync(int hotelId, int blockId)
    {
        var result = await _api.DeleteAsync($"{Base(hotelId)}/blocks/{blockId}");
        return (result.Success, result.Message);
    }

    public async Task<bool> MarkUpToDateAsync(int hotelId)
        => (await _api.PostAsync(Base(hotelId) + "/up-to-date")).Success;

    public async Task<(bool Success, string Message)> UpdateRoomPriceAsync(int hotelId, int roomId, decimal pricePerNight)
    {
        var result = await _api.PutAsync(
            $"{Base(hotelId)}/rooms/{roomId}/price",
            new UpdateRoomPriceDto { PricePerNight = pricePerNight });

        return (result.Success, result.Message);
    }

    public Task<AvailabilityCheckDto?> CheckBookingAsync(int bookingId)
        => _api.GetOrNullAsync<AvailabilityCheckDto>($"api/availability/check/{bookingId}");
}
