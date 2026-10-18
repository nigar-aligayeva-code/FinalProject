namespace HotelTravel.Application.DTOs.Auth;

// API login / register cavabı: JWT token + istifadəçi məlumatı
public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public DateTime ExpiresAtUtc { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public List<string> Roles { get; set; } = new();
}
