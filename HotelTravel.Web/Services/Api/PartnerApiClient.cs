using HotelTravel.Application.DTOs.Availability;

namespace HotelTravel.Web.Services.Api;

// Otel partnyoru: "mən hansı oteli idarə edirəm?" + admin üçün partnyor hesabları
public interface IPartnerApiClient
{
    Task<PartnerMeDto?> MeAsync();

    Task<List<PartnerAccountDto>> GetAllAsync();

    Task<(bool Success, string Message)> CreateAsync(CreatePartnerDto dto);

    Task<(bool Success, string Message)> RemoveAsync(string userId);
}

public class PartnerApiClient : IPartnerApiClient
{
    private readonly ApiClient _api;

    public PartnerApiClient(ApiClient api)
    {
        _api = api;
    }

    public Task<PartnerMeDto?> MeAsync()
        => _api.GetOrNullAsync<PartnerMeDto>("api/partner/me");

    public Task<List<PartnerAccountDto>> GetAllAsync()
        => _api.GetAsync<List<PartnerAccountDto>>("api/partners");

    public async Task<(bool Success, string Message)> CreateAsync(CreatePartnerDto dto)
    {
        var result = await _api.PostAsync("api/partners", dto);
        return (result.Success, result.Message);
    }

    public async Task<(bool Success, string Message)> RemoveAsync(string userId)
    {
        var result = await _api.DeleteAsync($"api/partners/{ApiUrl.Segment(userId)}");
        return (result.Success, result.Message);
    }
}
