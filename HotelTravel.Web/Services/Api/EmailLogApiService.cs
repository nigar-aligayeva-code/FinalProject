using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// IEmailLogService -> API (api/emails)
public class EmailLogApiService : IEmailLogService
{
    private readonly ApiClient _api;

    public EmailLogApiService(ApiClient api)
    {
        _api = api;
    }

    public Task<List<EmailLogDto>> GetRecentAsync(int take = 50)
        => _api.GetAsync<List<EmailLogDto>>($"api/emails?take={take}");

    public Task<EmailLogDto?> GetByIdAsync(int id)
        => _api.GetOrNullAsync<EmailLogDto>($"api/emails/{id}");
}
