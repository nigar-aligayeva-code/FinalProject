using HotelTravel.Application.DTOs.Amenities;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// IAmenityService -> API (api/amenities)
public class AmenityApiService : IAmenityService
{
    private readonly ApiClient _api;

    public AmenityApiService(ApiClient api)
    {
        _api = api;
    }

    public Task<List<GetAmenityDto>> GetAllAsync()
        => _api.GetAsync<List<GetAmenityDto>>("api/amenities");

    public Task<GetAmenityDto?> GetByIdAsync(int id)
        => _api.GetOrNullAsync<GetAmenityDto>($"api/amenities/{id}");

    public async Task CreateAsync(CreateAmenityDto dto)
        => ApiClient.EnsureOk(await _api.PostAsync("api/amenities", dto));

    public async Task<bool> UpdateAsync(int id, UpdateAmenityDto dto)
        => (await _api.PutAsync($"api/amenities/{id}", dto)).Success;

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var result = await _api.DeleteAsync($"api/amenities/{id}");
        return (result.Success, result.Message);
    }
}
