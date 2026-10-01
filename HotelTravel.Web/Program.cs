using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HotelTravel.Infrastructure.Seed;
namespace HotelTravel.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            // Database
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));
            
            // Identity
            builder.Services
                .AddIdentity<AppUser, IdentityRole>(options =>
                {
                    // Password qaydaları
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

            // Login olmayan user qorunan səhifəyə girəndə
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            // Dependency Injection
            builder.Services.AddScoped<IAppDbContext>(provider =>
                provider.GetRequiredService<AppDbContext>());

            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<IHotelService, HotelService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IHotelChainService, HotelChainService>();

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var roleManager =
                    scope.ServiceProvider
                        .GetRequiredService<RoleManager<IdentityRole>>();

                var userManager =
                    scope.ServiceProvider
                        .GetRequiredService<UserManager<AppUser>>();
                await IdentitySeeder.SeedAsync(
                    roleManager,
                    userManager,
                    builder.Configuration);
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            // Identity
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}