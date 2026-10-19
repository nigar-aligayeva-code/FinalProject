using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Profile;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Enter your current password.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a new password.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "The new password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm the new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
