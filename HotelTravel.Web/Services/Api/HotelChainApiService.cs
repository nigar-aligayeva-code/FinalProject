using HotelTravel.Application.DTOs.HotelChains;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// IHotelChainService -> API (api/hotel-chains)
public class HotelChainApiService : IHotelChainService
{
    private readonly ApiClient _api;

    public HotelChainApiService(ApiClient api)
    {
        _api = api;
    }

    public Task<List<GetHotelChainDto>> GetAllAsync()
        => _api.GetAsync<List<GetHotelChainDto>>("api/hotel-chains");

    public Task<GetHotelChainDto?> GetByIdAsync(int id)
        => _api.GetOrNullAsync<GetHotelChainDto>($"api/hotel-chains/{id}");

    public async Task CreateAsync(CreateHotelChainDto dto)
        => ApiClient.EnsureOk(await _api.PostAsync("api/hotel-chains", dto));

    public async Task<bool> UpdateAsync(int id, UpdateHotelChainDto dto)
        => (await _api.PutAsync($"api/hotel-chains/{id}", dto)).Success;

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var result = await _api.DeleteAsync($"api/hotel-chains/{id}");
        return (result.Success, result.Message);
    }
}
