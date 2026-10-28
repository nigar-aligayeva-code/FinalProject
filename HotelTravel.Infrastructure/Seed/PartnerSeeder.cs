using HotelTravel.Domain.Entities;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HotelTravel.Infrastructure.Seed;

/// <summary>
/// "HotelPartner" rolu həmişə yaradılır.
/// Yalnız lokal işdə (Development) demo partnyor hesabları da yaradılır ki, iki hal göstərilsin:
///   • partnyorlu otellər - sorğunu otel işçisi özü təsdiqləyir;
///   • partnyorsuz otellər (Bakının iki oteli) - sorğuya admin baxır, otelə zəng edir.
/// Bütün demo hesabların parolu eynidir ("DemoPartner:Password", standart "Partner123").
/// </summary>
public static class PartnerSeeder
{
    public const string PartnerRole = "HotelPartner";

    // Əvvəlki versiyada demo partnyor bu otelə bağlı idi (indi Bakı otelləri partnyorsuzdur)
    private const string LegacyHotelName = "Caspian Grand Hotel";

    // Otel -> demo partnyorun email-i (Bakı otelləri qəsdən siyahıda yoxdur)
    public static readonly IReadOnlyList<(string HotelName, string Email, string FirstName)> DemoPartners = new[]
    {
        ("Bosphorus Palace Hotel", "partner@tripio.az", "Bosphorus"),
        ("Gabala Mountain Resort", "partner.gabala@tripio.az", "Gabala"),
        ("Antalya Blue Resort", "partner.antalya@tripio.az", "Antalya"),
        ("Tbilisi Heritage Hotel", "partner.tbilisi@tripio.az", "Tbilisi"),
        ("Batumi Sea View Hotel", "partner.batumi@tripio.az", "Batumi"),
        ("Dubai Marina Luxury Hotel", "partner.dubai@tripio.az", "Dubai")
    };

    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<AppUser> userManager,
        AppDbContext context,
        IConfiguration configuration,
        bool createDemoAccounts)
    {
        if (!await roleManager.RoleExistsAsync(PartnerRole))
            await roleManager.CreateAsync(new IdentityRole(PartnerRole));

        if (!createDemoAccounts)
            return;

        var password = configuration["DemoPartner:Password"] ?? "Partner123";

        // Eyni adlı iki otel ola bilər (admin yaradıb) - birincini götürürük
        var hotels = (await context.Hotels
                .Where(h => !h.IsDeleted)
                .OrderBy(h => h.Id)
                .Select(h => new { h.Id, h.Name })
                .ToListAsync())
            .GroupBy(h => h.Name)
            .ToDictionary(g => g.Key, g => g.First().Id);

        foreach (var (hotelName, email, firstName) in DemoPartners)
        {
            if (!hotels.TryGetValue(hotelName, out var hotelId))
                continue;

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new AppUser
                {
                    FirstName = firstName,
                    LastName = "Reception",
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true,
                    PartnerHotelId = hotelId
                };

                var created = await userManager.CreateAsync(user, password);

                if (created.Succeeded)
                    await userManager.AddToRoleAsync(user, PartnerRole);

                continue;
            }

            // Köhnə demo: partner@tripio.az əvvəl Caspian Grand-a bağlı idi -> indi Bosphorus Palace.
            // Admin-in sonradan etdiyi dəyişikliklərə (başqa otel, icazənin götürülməsi) toxunmuruq.
            if (hotels.TryGetValue(LegacyHotelName, out var legacyId) &&
                user.PartnerHotelId == legacyId &&
                await userManager.IsInRoleAsync(user, PartnerRole))
            {
                user.PartnerHotelId = hotelId;
                user.FirstName = firstName;
                await userManager.UpdateAsync(user);
            }
        }
    }
}
