using System.Security.Claims;

namespace HotelTravel.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    // JWT-dəki "sub" claim-i NameIdentifier kimi gəlir
    public static string? GetUserId(this ClaimsPrincipal user)
        => user.Identity?.IsAuthenticated == true
            ? user.FindFirstValue(ClaimTypes.NameIdentifier)
              ?? user.FindFirstValue("sub")
            : null;
}
