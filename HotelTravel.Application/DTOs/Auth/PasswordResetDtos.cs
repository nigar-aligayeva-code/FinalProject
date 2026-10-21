using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Auth;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Məktubdakı linkdən gəlir (Base64Url)
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a new password.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "The new password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm the new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
