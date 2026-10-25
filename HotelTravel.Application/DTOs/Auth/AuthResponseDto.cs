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

    // Seçilmiş avatar (null -> baş hərflər)
    public string? AvatarKey { get; set; }

    // E-poçt təsdiqlənibmi (qeydiyyatdan sonra məktubdakı link ilə)
    public bool EmailConfirmed { get; set; }

    public List<string> Roles { get; set; } = new();
}
