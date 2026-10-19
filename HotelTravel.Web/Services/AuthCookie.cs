using System.Security.Claims;
using HotelTravel.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HotelTravel.Web.Services;

// API-dən gələn istifadəçi məlumatından cookie üçün ClaimsPrincipal yaradır
// (login-də və profil yenilənəndən sonra istifadə olunur)
public static class AuthCookie
{
    public static ClaimsPrincipal BuildPrincipal(AuthResponseDto auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.UserId),
            new(ClaimTypes.Name, auth.Email),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.GivenName, auth.FirstName),
            new(ClaimTypes.Surname, auth.LastName),
            new(AuthClaimTypes.AccessToken, auth.AccessToken)
        };

        if (!string.IsNullOrWhiteSpace(auth.PhoneNumber))
            claims.Add(new Claim(ClaimTypes.MobilePhone, auth.PhoneNumber));

        if (!string.IsNullOrWhiteSpace(auth.AvatarKey))
            claims.Add(new Claim(AuthClaimTypes.Avatar, auth.AvatarKey));

        claims.AddRange(auth.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }
}
