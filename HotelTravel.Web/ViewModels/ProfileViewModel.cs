using HotelTravel.Application.DTOs.Auth;
using HotelTravel.Application.DTOs.Profile;

namespace HotelTravel.Web.ViewModels;

public class ProfileViewModel
{
    public AuthResponseDto User { get; set; } = new();

    public ProfileSummaryDto Summary { get; set; } = new();

    public UpdateProfileDto Profile { get; set; } = new();

    public ChangePasswordDto Password { get; set; } = new();

    // Hansı tab açıq olsun: "details" və ya "security"
    public string ActiveTab { get; set; } = "details";
}
