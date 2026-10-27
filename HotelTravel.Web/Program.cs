using System.Globalization;
using HotelTravel.Application.Services;
using HotelTravel.Web.Filters;
using HotelTravel.Web.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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

                // İstifadəçiyə gedən mesajları (ModelState, TempData, ViewBag) seçilmiş dilə çevirir
                options.Filters.Add<LocalizeMessagesFilter>();

                // ErrorMessage yazılmamış [Required] və s. üçün də tərcümə olunan standart mesaj
                options.ModelMetadataDetailsProviders.Add(new DefaultValidationMessages());
            })
            // Form xətaları və [Display] adları Resources/SharedResource.*.resx-dən tərcümə olunur
            .AddDataAnnotationsLocalization(options =>
            {
                options.DataAnnotationLocalizerProvider = (_, factory) =>
                    factory.Create(typeof(SharedResource));
            });

            // Model binding mesajları ("The value 'abc' is not valid.") da tərcümə olunsun
            builder.Services.AddOptions<MvcOptions>()
                .Configure<IStringLocalizerFactory>((options, factory) =>
                {
                    var t = factory.Create(typeof(SharedResource));
                    var m = options.ModelBindingMessageProvider;

                    m.SetValueMustNotBeNullAccessor(v => t["The value '{0}' is invalid.", v]);
                    m.SetAttemptedValueIsInvalidAccessor((v, f) => t["The value '{0}' is not valid for {1}.", v, f]);
                    m.SetValueIsInvalidAccessor(v => t["The value '{0}' is invalid.", v]);
                    m.SetMissingBindRequiredValueAccessor(f => t["A value for the '{0}' parameter or property was not provided.", f]);
                    m.SetValueMustBeANumberAccessor(f => t["The field {0} must be a number.", f]);
                    m.SetNonPropertyValueMustBeANumberAccessor(() => t["The field must be a number."]);
                });

            // =========================================
            // API CLIENTS (HttpClient)
            // =========================================
            // MVC verilənlər bazasına müraciət etmir:
            // bütün məlumat HotelTravel.API-dən gəlir.

            builder.Services.AddTripioApiClients(builder.Configuration);

            // =========================================
            // ÇOXDİLLİLİK (EN / AZ / RU)
            // =========================================
            // Mətnlər Resources/SharedResource.{az,ru}.resx fayllarındadır.
            // Rəqəm və tarix formatı həmişə en-US qalır (qiymət "120.50" pozulmasın),
            // yalnız UI dili dəyişir. Seçim cookie-də saxlanılır.

            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            // Böyük/kiçik hərfli eyni sözlər də tərcümə olunsun ("Guest" / "guest")
            builder.Services.AddTransient<
                Microsoft.Extensions.Localization.IStringLocalizer<HotelTravel.Web.SharedResource>,
                HotelTravel.Web.Localization.SharedLocalizer>();

            // Azərbaycan və rus hərfləri HTML-də "&#x259;" kimi yox, olduğu kimi yazılsın
            builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options =>
                options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(
                    System.Text.Unicode.UnicodeRanges.All));

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-US", LanguageRules.Default);
                options.SupportedCultures = new List<CultureInfo> { new("en-US") };
                options.SupportedUICultures = LanguageRules.Codes.Select(c => new CultureInfo(c)).ToList();
            });

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

            app.UseRequestLocalization();

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
