using HotelTravel.Application.DTOs.CityGuide;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// ICityGuideService -> API (api/city-guide)
public class CityGuideApiService : ICityGuideService
{
    private readonly ApiClient _api;

    public CityGuideApiService(ApiClient api)
    {
        _api = api;
    }

    public async Task<List<CityPlaceDto>> GetByCityAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return new List<CityPlaceDto>();

        return await _api.GetAsync<List<CityPlaceDto>>($"api/city-guide/city/{ApiUrl.Segment(city)}");
    }

    public Task<List<string>> GetCitiesAsync()
        => _api.GetAsync<List<string>>("api/city-guide/cities");

    public Task<List<CityPlaceDto>> GetAllAsync(string? city)
        => _api.GetAsync<List<CityPlaceDto>>(ApiUrl.For("api/city-guide").Add("city", city));

    public Task<SaveCityPlaceDto?> GetForEditAsync(int id)
        => _api.GetOrNullAsync<SaveCityPlaceDto>($"api/city-guide/{id}/edit");

    public async Task SaveAsync(SaveCityPlaceDto dto)
        => ApiClient.EnsureOk(await _api.PostAsync("api/city-guide", dto));

    public async Task<bool> DeleteAsync(int id)
        => (await _api.DeleteAsync($"api/city-guide/{id}")).Success;
}
