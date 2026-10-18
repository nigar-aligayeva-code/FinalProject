using HotelTravel.Application.DTOs.PromoCodes;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// IPromoCodeService -> API (api/promo-codes)
public class PromoCodeApiService : IPromoCodeService
{
    private readonly ApiClient _api;

    public PromoCodeApiService(ApiClient api)
    {
        _api = api;
    }

    public Task<List<GetPromoCodeDto>> GetAllAsync()
        => _api.GetAsync<List<GetPromoCodeDto>>("api/promo-codes");

    public async Task<(bool Success, string Message)> CreateAsync(CreatePromoCodeDto dto)
    {
        var result = await _api.PostAsync("api/promo-codes", dto);
        return (result.Success, result.Message);
    }

    public async Task<bool> ToggleActiveAsync(int id)
        => (await _api.PostAsync($"api/promo-codes/{id}/toggle")).Success;

    public async Task<bool> DeleteAsync(int id)
        => (await _api.DeleteAsync($"api/promo-codes/{id}")).Success;
}
