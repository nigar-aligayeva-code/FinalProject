using HotelTravel.Application.DTOs.HotelChains;

namespace HotelTravel.Application.Interfaces;

public interface IHotelChainService
{
    Task<List<GetHotelChainDto>> GetAllAsync();

    Task<GetHotelChainDto?> GetByIdAsync(int id);

    Task CreateAsync(CreateHotelChainDto dto);

    Task<bool> UpdateAsync(
        int id,
        UpdateHotelChainDto dto);

    Task<bool> DeleteAsync(int id);
}