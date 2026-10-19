using HotelTravel.Application.DTOs.Email;

namespace HotelTravel.Application.Interfaces;

public interface IEmailLogService
{
    Task<List<EmailLogDto>> GetRecentAsync(int take = 50);

    Task<EmailLogDto?> GetByIdAsync(int id);
}
