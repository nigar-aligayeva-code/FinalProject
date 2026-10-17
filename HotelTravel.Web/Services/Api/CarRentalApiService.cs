using HotelTravel.Application.DTOs.Cars;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Web.Services.Api;

// ICarRentalService -> API (api/cars)
public class CarRentalApiService : ICarRentalService
{
    private readonly ApiClient _api;

    public CarRentalApiService(ApiClient api)
    {
        _api = api;
    }

    // SAYT
    public Task<List<string>> GetCitiesAsync()
        => _api.GetAsync<List<string>>("api/cars/cities");

    public async Task<List<CarDto>> SearchAsync(string city, DateTime? pickupDate, DateTime? returnDate)
    {
        if (string.IsNullOrWhiteSpace(city))
            return new List<CarDto>();

        return await _api.GetAsync<List<CarDto>>(
            ApiUrl.For("api/cars/search")
                .Add("city", city)
                .Add("pickupDate", pickupDate)
                .Add("returnDate", returnDate));
    }

    public Task<CarDto?> GetCarAsync(int id, DateTime? pickupDate, DateTime? returnDate)
        => _api.GetOrNullAsync<CarDto>(
            ApiUrl.For($"api/cars/{id}")
                .Add("pickupDate", pickupDate)
                .Add("returnDate", returnDate));

    public async Task<(bool Success, string Message, string? ConfirmationCode)>
        BookAsync(CreateCarRentalDto dto, string? appUserId)
    {
        var result = await _api.PostAsync("api/cars/rentals", dto);
        return (result.Success, result.Message, result.Code);
    }

    public async Task<CarRentalDto?> GetByCodeAsync(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return null;

        return await _api.GetOrNullAsync<CarRentalDto>(
            $"api/cars/rentals/by-code/{ApiUrl.Segment(confirmationCode)}");
    }

    public async Task<List<CarRentalDto>> GetByUserAsync(string appUserId)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return new List<CarRentalDto>();

        return await _api.GetAsync<List<CarRentalDto>>("api/cars/rentals/mine");
    }

    public async Task<(bool Success, string Message)> CancelByUserAsync(int rentalId, string appUserId)
    {
        var result = await _api.PostAsync($"api/cars/rentals/{rentalId}/cancel-mine");
        return (result.Success, result.Message);
    }

    // ADMIN
    public Task<List<CarDto>> GetFleetAsync(string? city)
        => _api.GetAsync<List<CarDto>>(ApiUrl.For("api/cars/fleet").Add("city", city));

    public Task<SaveCarDto?> GetCarForEditAsync(int id)
        => _api.GetOrNullAsync<SaveCarDto>($"api/cars/{id}/edit");

    public async Task SaveCarAsync(SaveCarDto dto)
        => ApiClient.EnsureOk(await _api.PostAsync("api/cars/fleet", dto));

    public async Task<bool> ToggleCarAsync(int id)
        => (await _api.PostAsync($"api/cars/{id}/toggle")).Success;

    public Task<List<CarRentalDto>> GetRentalsAsync()
        => _api.GetAsync<List<CarRentalDto>>("api/cars/rentals");

    public async Task<bool> SetRentalStatusAsync(int id, CarRentalStatus status)
        => (await _api.PutAsync(
            $"api/cars/rentals/{id}/status",
            new StatusUpdateDto<CarRentalStatus> { Status = status })).Success;
}
