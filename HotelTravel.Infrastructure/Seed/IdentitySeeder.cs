using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace HotelTravel.Infrastructure.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<AppUser> userManager,
        IConfiguration configuration)
    {
        // ROLES
        string[] roles =
        {
            "Admin",
            "User"
        };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult =
                    await roleManager.CreateAsync(
                        new IdentityRole(roleName));

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        roleResult.Errors.Select(e => e.Description));

                    throw new Exception(
                        $"Could not create role '{roleName}': {errors}");
                }
            }
        }

        // ADMIN SETTINGS
        var adminEmail =
            configuration["AdminSettings:Email"];

        var adminPassword =
            configuration["AdminSettings:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new Exception(
                "Admin email or password is not configured.");
        }

        // ADMIN
        var admin =
            await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new AppUser
            {
                FirstName = "HotelHub",
                LastName = "Admin",
                Email = adminEmail,
                UserName = adminEmail,
                EmailConfirmed = true
            };

            var createResult =
                await userManager.CreateAsync(
                    admin,
                    adminPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    createResult.Errors.Select(e => e.Description));

                throw new Exception(
                    $"Could not create admin: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(
                admin,
                "Admin"))
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    admin,
                    "Admin");

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    roleResult.Errors.Select(e => e.Description));

                throw new Exception(
                    $"Could not add Admin role: {errors}");
            }
        }
    }
}