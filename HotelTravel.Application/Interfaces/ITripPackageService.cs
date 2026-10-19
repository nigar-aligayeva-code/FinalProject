using HotelTravel.Application.DTOs.Trips;

namespace HotelTravel.Application.Interfaces;

public interface ITripPackageService
{
    // Uçuş olan şəhərlər (haradan)
    Task<List<string>> GetOriginsAsync();

    // Həm uçuşu, həm oteli olan şəhərlər (haraya)
    Task<List<string>> GetDestinationsAsync();

    Task<TripPlanDto> PlanAsync(TripSearchDto search);

    Task<(bool Success, string Message, string? Code)> BookAsync(CreateTripPackageDto dto, string appUserId);

    Task<TripPackageDto?> GetByCodeAsync(string code, string appUserId);

    Task<List<TripPackageDto>> GetByUserAsync(string appUserId);
}
