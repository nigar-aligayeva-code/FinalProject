using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Infrastructure.Persistence;
using HotelTravel.Infrastructure.Services;
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

        return services;
    }
}
