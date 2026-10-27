using System.Security.Claims;
using HotelTravel.API.Extensions;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HotelTravel.API.Services;

/// <summary>
/// Kim hansı oteli idarə edə bilər?
///   • Admin - bütün otelləri;
///   • HotelPartner - yalnız öz otelini (AppUser.PartnerHotelId).
/// Otel bazadan oxunur (token-dən yox), ona görə admin partnyoru başqa otelə keçirəndə dərhal təsir edir.
/// </summary>
public class PartnerScope
{
    public const string PartnerRole = "HotelPartner";

    private readonly UserManager<AppUser> _users;

    public PartnerScope(UserManager<AppUser> users)
    {
        _users = users;
    }

    public async Task<int?> HotelIdOfAsync(ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId();

        if (string.IsNullOrWhiteSpace(userId) || !principal.IsInRole(PartnerRole))
            return null;

        var user = await _users.FindByIdAsync(userId);

        return user?.PartnerHotelId;
    }

    public async Task<bool> CanManageHotelAsync(ClaimsPrincipal principal, int hotelId)
    {
        if (principal.IsInRole("Admin"))
            return true;

        return await HotelIdOfAsync(principal) == hotelId;
    }
}
