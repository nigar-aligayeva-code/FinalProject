using HotelTravel.Application.DTOs.PromoCodes;

namespace HotelTravel.Application.Interfaces;

public interface IPromoCodeService
{
    Task<List<GetPromoCodeDto>> GetAllAsync();

    Task<(bool Success, string Message)> CreateAsync(CreatePromoCodeDto dto);

    Task<bool> ToggleActiveAsync(int id);

    Task<bool> DeleteAsync(int id);
}
