using HotelTravel.Application.DTOs.CityGuide;

namespace HotelTravel.Application.Interfaces;

public interface ICityGuideService
{
    // Saytda: bir şəhərin bələdçisi
    Task<List<CityPlaceDto>> GetByCityAsync(string city);

    // Bələdçisi olan şəhərlər (Attractions səhifəsi üçün)
    Task<List<string>> GetCitiesAsync();

    // Admin
    Task<List<CityPlaceDto>> GetAllAsync(string? city);
    Task<SaveCityPlaceDto?> GetForEditAsync(int id);
    Task SaveAsync(SaveCityPlaceDto dto);
    Task<bool> DeleteAsync(int id);
}
