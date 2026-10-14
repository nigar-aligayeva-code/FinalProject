using HotelTravel.Application.DTOs.Cars;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Interfaces;

public interface ICarRentalService
{
    // Saytda
    Task<List<string>> GetCitiesAsync();

    Task<List<CarDto>> SearchAsync(string city, DateTime? pickupDate, DateTime? returnDate);

    Task<CarDto?> GetCarAsync(int id, DateTime? pickupDate, DateTime? returnDate);

    Task<(bool Success, string Message, string? ConfirmationCode)>
        BookAsync(CreateCarRentalDto dto, string? appUserId);

    Task<CarRentalDto?> GetByCodeAsync(string confirmationCode);

    Task<List<CarRentalDto>> GetByUserAsync(string appUserId);

    Task<(bool Success, string Message)> CancelByUserAsync(int rentalId, string appUserId);

    // Admin
    Task<List<CarDto>> GetFleetAsync(string? city);
    Task<SaveCarDto?> GetCarForEditAsync(int id);
    Task SaveCarAsync(SaveCarDto dto);
    Task<bool> ToggleCarAsync(int id);
    Task<List<CarRentalDto>> GetRentalsAsync();
    Task<bool> SetRentalStatusAsync(int id, CarRentalStatus status);
}
