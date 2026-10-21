using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Infrastructure.Persistence;
using HotelTravel.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelTravel.Infrastructure;

// Verilənlər bazası + bütün biznes servisləri bir yerdə qeydiyyatdan keçir.
// API-nin Program.cs faylı yalnız bunu çağırır.
public static class DependencyInjection
{
    public static IServiceCollection AddHotelTravelInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing.");
        }

        // SQL Express kompüter yenicə açılanda gec cavab verir:
        // standart 15 saniyə əvəzinə 60 saniyə gözləyirik
        connectionString = WithLongerTimeout(connectionString);

        // DATABASE
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Rezervasiya tranzaksiyaları üçün (deadlock retry)
        services.AddDbContextFactory<AppDbContext>(
            options => options.UseSqlServer(connectionString),
            lifetime: ServiceLifetime.Scoped);

        services.AddScoped<IAppDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        // SERVICES
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingCreationService, BookingCreationService>();
        services.AddScoped<IHotelChainService, HotelChainService>();
        services.AddScoped<IAmenityService, AmenityService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IPromoCodeService, PromoCodeService>();
        services.AddScoped<ICityGuideService, CityGuideService>();
        services.AddScoped<ICarRentalService, CarRentalService>();
        services.AddScoped<ITaxiService, TaxiService>();
        services.AddScoped<IFlightService, FlightService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IProfileService, ProfileService>();

        // EMAIL + PDF
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailOutbox, EmailOutbox>();
        services.AddSingleton<IVoucherPdfService, QuestPdfVoucherService>();
        services.AddScoped<IBookingEmailService, BookingEmailService>();
        services.AddScoped<IEmailLogService, EmailLogService>();

        // TRIP PACKAGE
        services.AddScoped<ITripPackageService, TripPackageService>();

        // CANLI BİLDİRİŞLƏR (IRealtimePublisher API-də qeydiyyatdan keçir)
        services.AddScoped<IActivityNotifier, ActivityNotifier>();

        return services;
    }

    private static string WithLongerTimeout(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);

        if (builder.ConnectTimeout < 60)
            builder.ConnectTimeout = 60;

        return builder.ConnectionString;
    }
}
