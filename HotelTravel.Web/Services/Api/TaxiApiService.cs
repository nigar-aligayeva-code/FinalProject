using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Taxis;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Web.Services.Api;

// ITaxiService -> API (api/taxis)
public class TaxiApiService : ITaxiService
{
    private readonly ApiClient _api;

    public TaxiApiService(ApiClient api)
    {
        _api = api;
    }

    public Task<List<TaxiHotelDto>> GetHotelsAsync()
        => _api.GetAsync<List<TaxiHotelDto>>("api/taxis/hotels");

    public Task<TaxiHotelDto?> GetHotelAsync(int hotelId)
        => _api.GetOrNullAsync<TaxiHotelDto>($"api/taxis/hotels/{hotelId}");

    // Qiymət cədvəli sabit qaydalardan hesablanır (TaxiRules), verilənlər bazası lazım deyil.
    // Eyni nəticəni API də verir: GET api/taxis/options
    public List<TaxiOptionDto> GetOptions(int distanceKm, int passengers, int luggage)
        => TaxiRules.Vehicles
            .Select(v => new TaxiOptionDto
            {
                Type = v.Type,
                Name = v.Name,
                Example = v.Example,
                MaxPassengers = v.MaxPassengers,
                MaxLuggage = v.MaxLuggage,
                Price = TaxiRules.CalculatePrice(v.Type, distanceKm),
                Fits = TaxiRules.Fits(v.Type, passengers, luggage)
            })
            .OrderBy(o => o.Price)
            .ToList();

    public async Task<(bool Success, string Message, string? ConfirmationCode)>
        BookAsync(CreateTaxiBookingDto dto, string? appUserId)
    {
        var result = await _api.PostAsync("api/taxis/bookings", dto);
        return (result.Success, result.Message, result.Code);
    }

    public async Task<TaxiBookingDto?> GetByCodeAsync(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return null;

        return await _api.GetOrNullAsync<TaxiBookingDto>(
            $"api/taxis/bookings/by-code/{ApiUrl.Segment(confirmationCode)}");
    }

    public async Task<List<TaxiBookingDto>> GetByUserAsync(string appUserId)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return new List<TaxiBookingDto>();

        return await _api.GetAsync<List<TaxiBookingDto>>("api/taxis/bookings/mine");
    }

    public async Task<(bool Success, string Message)> CancelByUserAsync(int taxiBookingId, string appUserId)
    {
        var result = await _api.PostAsync($"api/taxis/bookings/{taxiBookingId}/cancel-mine");
        return (result.Success, result.Message);
    }

    public Task<List<TaxiBookingDto>> GetAllAsync(TaxiBookingStatus? status)
        => _api.GetAsync<List<TaxiBookingDto>>(ApiUrl.For("api/taxis/bookings").Add("status", status));

    public async Task<bool> SetStatusAsync(int id, TaxiBookingStatus status)
        => (await _api.PutAsync(
            $"api/taxis/bookings/{id}/status",
            new StatusUpdateDto<TaxiBookingStatus> { Status = status })).Success;
}
