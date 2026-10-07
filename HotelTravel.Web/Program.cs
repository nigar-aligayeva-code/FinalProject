using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Infrastructure.Persistence;
using HotelTravel.Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();


            // =========================================
            // DATABASE
            // =========================================

            builder.Services.AddDbContext<AppDbContext>(
                options =>
                    options.UseSqlServer(
                        builder.Configuration
                            .GetConnectionString(
                                "DefaultConnection")));


            // =========================================
            // IDENTITY
            // =========================================

            builder.Services
                .AddIdentity<AppUser, IdentityRole>(
                    options =>
                    {
                        // Password rules
                        options.Password.RequireDigit = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequiredLength = 6;

                        // Email
                        options.User.RequireUniqueEmail = true;
                    })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();


            // =========================================
            // COOKIE SETTINGS
            // =========================================

            builder.Services.ConfigureApplicationCookie(
                options =>
                {
                    options.LoginPath =
                        "/Account/Login";

                    options.AccessDeniedPath =
                        "/Account/AccessDenied";
                });


            // =========================================
            // DEPENDENCY INJECTION
            // =========================================

            builder.Services.AddScoped<IAppDbContext>(
                provider =>
                    provider.GetRequiredService<AppDbContext>());

            builder.Services.AddScoped<
                IRoomService,
                RoomService>();

            builder.Services.AddScoped<
                IHotelService,
                HotelService>();

            builder.Services.AddScoped<
                IBookingService,
                BookingService>();

            builder.Services.AddScoped<
                IHotelChainService,
                HotelChainService>();

            builder.Services.AddScoped<
                IAmenityService,
                AmenityService>();


            var app = builder.Build();


            // =========================================
            // SEED DATA
            // =========================================

            using (var scope =
                   app.Services.CreateScope())
            {
                var services =
                    scope.ServiceProvider;


                // -----------------------------
                // IDENTITY SEED
                // -----------------------------

                var roleManager =
                    services.GetRequiredService<
                        RoleManager<IdentityRole>>();

                var userManager =
                    services.GetRequiredService<
                        UserManager<AppUser>>();

                await IdentitySeeder.SeedAsync(
                    roleManager,
                    userManager,
                    builder.Configuration);


                // -----------------------------
                // HOTEL DATA SEED
                // -----------------------------

                var context =
                    services.GetRequiredService<
                        AppDbContext>();

                await HotelDataSeeder.SeedAsync(
                    context);
            }


            // =========================================
            // HTTP PIPELINE
            // =========================================

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler(
                    "/Home/Error");

                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();


            // =========================================
            // IDENTITY
            // =========================================

            app.UseAuthentication();

            app.UseAuthorization();


            // =========================================
            // ROUTING
            // =========================================

            app.MapControllerRoute(
                name: "default",
                pattern:
                    "{controller=Home}/{action=Index}/{id?}");


            app.Run();
        }
    }
}