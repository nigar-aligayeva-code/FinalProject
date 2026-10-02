using HotelTravel.Application.DTOs.Amenities;

namespace HotelTravel.Application.Interfaces;

public interface IAmenityService
{
    Task<List<GetAmenityDto>> GetAllAsync();

    Task<GetAmenityDto?> GetByIdAsync(int id);

    Task CreateAsync(CreateAmenityDto dto);

    Task<bool> UpdateAsync(
        int id,
        UpdateAmenityDto dto);

    Task<(bool Success, string Message)> DeleteAsync(int id);
}