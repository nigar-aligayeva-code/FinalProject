using HotelTravel.Application.DTOs.Profile;

namespace HotelTravel.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileSummaryDto> GetSummaryAsync(string userId);
}
