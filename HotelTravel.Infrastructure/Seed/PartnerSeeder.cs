using HotelTravel.Domain.Entities;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HotelTravel.Infrastructure.Seed;

/// <summary>
/// "HotelPartner" rolu həmişə yaradılır.
/// Yalnız lokal işdə (Development) bir demo partnyor hesabı da yaradılır ki,
/// partnyor panelini dərhal yoxlamaq olsun: Caspian Grand Hotel-in işçisi.
/// Email / parol appsettings-də "DemoPartner" bölməsi ilə dəyişdirilə bilər.
/// </summary>
public static class PartnerSeeder
{
    public const string PartnerRole = "HotelPartner";
    public const string DemoHotelName = "Caspian Grand Hotel";

    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<AppUser> userManager,
        AppDbContext context,
        IConfiguration configuration,
        bool createDemoAccount)
    {
        if (!await roleManager.RoleExistsAsync(PartnerRole))
            await roleManager.CreateAsync(new IdentityRole(PartnerRole));

        if (!createDemoAccount)
            return;

        var email = configuration["DemoPartner:Email"] ?? "partner@tripio.az";
        var password = configuration["DemoPartner:Password"] ?? "Partner123";

        if (await userManager.FindByEmailAsync(email) != null)
            return;

        var hotel = await context.Hotels
            .FirstOrDefaultAsync(h => h.Name == DemoHotelName && !h.IsDeleted);

        if (hotel is null)
            return;

        var user = new AppUser
        {
            FirstName = "Caspian",
            LastName = "Reception",
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            PartnerHotelId = hotel.Id
        };

        var created = await userManager.CreateAsync(user, password);

        if (created.Succeeded)
            await userManager.AddToRoleAsync(user, PartnerRole);
    }
}
