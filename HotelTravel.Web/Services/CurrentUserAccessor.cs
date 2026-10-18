using System.Security.Claims;
using HotelTravel.Domain.Entities;

namespace HotelTravel.Web.Services;

public static class AuthClaimTypes
{
    // Cookie-də saxlanan JWT token
    public const string AccessToken = "api_access_token";
}

// Daxil olmuş istifadəçinin məlumatı cookie-dəki claim-lərdən oxunur
// (MVC verilənlər bazasına müraciət etmir; məlumat login zamanı API-dən gəlib)
public class CurrentUserAccessor
{
    public string? GetUserId(ClaimsPrincipal user)
        => user.Identity?.IsAuthenticated == true
            ? user.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    public Task<AppUser?> GetUserAsync(ClaimsPrincipal user)
    {
        var id = GetUserId(user);

        if (id == null)
            return Task.FromResult<AppUser?>(null);

        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name ?? string.Empty;

        return Task.FromResult<AppUser?>(new AppUser
        {
            Id = id,
            Email = email,
            UserName = email,
            FirstName = user.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
            LastName = user.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
            PhoneNumber = user.FindFirstValue(ClaimTypes.MobilePhone)
        });
    }

    public bool IsAdmin(ClaimsPrincipal user) => user.IsInRole("Admin");
}
