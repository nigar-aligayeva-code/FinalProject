using HotelTravel.Application.DTOs.Auth;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Profile;

namespace HotelTravel.Web.Services.Api;

public interface IProfileApiClient
{
    Task<AuthResponseDto> GetMeAsync();
    Task<ProfileSummaryDto> GetSummaryAsync();
    Task<ApiResultDto> UpdateAsync(UpdateProfileDto dto);
    Task<ApiResultDto> ChangePasswordAsync(ChangePasswordDto dto);
    Task<ApiResultDto> UpdateAvatarAsync(string? avatarKey);
}

// Profil -> API (api/auth/me, api/profile)
public class ProfileApiClient : IProfileApiClient
{
    private readonly ApiClient _api;

    public ProfileApiClient(ApiClient api)
    {
        _api = api;
    }

    public Task<AuthResponseDto> GetMeAsync()
        => _api.GetAsync<AuthResponseDto>("api/auth/me");

    public Task<ProfileSummaryDto> GetSummaryAsync()
        => _api.GetAsync<ProfileSummaryDto>("api/profile/summary");

    public Task<ApiResultDto> UpdateAsync(UpdateProfileDto dto)
        => _api.PutAsync("api/profile", dto);

    public Task<ApiResultDto> ChangePasswordAsync(ChangePasswordDto dto)
        => _api.PostAsync("api/profile/change-password", dto);

    public Task<ApiResultDto> UpdateAvatarAsync(string? avatarKey)
        => _api.PutAsync("api/profile/avatar", new UpdateAvatarDto { AvatarKey = avatarKey });
}
