using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Auth;

// Məktubdakı təsdiq linkindən gələn məlumat
public class ConfirmEmailDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}
