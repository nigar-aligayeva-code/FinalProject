using HotelTravel.Application.DTOs.Hotels;

namespace HotelTravel.Application.Interfaces;

public interface IHotelService
{
    Task<List<GetHotelDto>> GetAllAsync();
    Task<GetHotelDto?> GetByIdAsync(int id);
    Task CreateAsync(CreateHotelDto dto);
    Task<bool> UpdateAsync(int id, UpdateHotelDto dto);
    Task<bool> DeleteAsync(int id);
}