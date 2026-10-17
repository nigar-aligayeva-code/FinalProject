using HotelTravel.Web.Filters;
using HotelTravel.Web.Services;
using HotelTravel.Web.Services.Api;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HotelTravel.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =========================================
            // MVC
            // =========================================

            builder.Services.AddControllersWithViews(options =>
            {
                // API xətaları (401, API işləmir və s.) üçün səliqəli cavab
                options.Filters.Add<ApiExceptionFilter>();
            });

            // =========================================
            // API CLIENTS (HttpClient)
            // =========================================
            // MVC verilənlər bazasına müraciət etmir:
            // bütün məlumat HotelTravel.API-dən gəlir.

            builder.Services.AddTripioApiClients(builder.Configuration);

            builder.Services.AddScoped<CurrentUserAccessor>();

            // =========================================
            // AUTHENTICATION (cookie + API JWT)
            // =========================================
            // Login zamanı API JWT token qaytarır; token cookie-də saxlanır
            // və hər API sorğusuna ApiTokenHandler tərəfindən əlavə olunur.

            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.Cookie.Name = "Tripio.Auth";
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);

                    // Token-in müddəti uzanmır, ona görə cookie də uzanmasın
                    options.SlidingExpiration = false;
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // =========================================
            // HTTP PIPELINE
            // =========================================

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // =========================================
            // MVC ROUTING
            // =========================================

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
