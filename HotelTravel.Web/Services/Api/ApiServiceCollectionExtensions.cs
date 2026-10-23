using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

public static class ApiServiceCollectionExtensions
{
    // MVC-nin bütün servisləri API-ni HttpClient ilə çağıran versiyalardır.
    // Controller-lər yenə IRoomService, IBookingService və s. istifadə edir.
    public static IServiceCollection AddTripioApiClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Api:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Api:BaseUrl is missing in appsettings.json.");

        if (!baseUrl.EndsWith('/'))
            baseUrl += "/";

        var baseUri = new Uri(baseUrl);

        services.AddHttpContextAccessor();
        services.AddTransient<ApiTokenHandler>();

        // Token-li ümumi client
        services
            .AddHttpClient<ApiClient>(client =>
            {
                client.BaseAddress = baseUri;
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ApiTokenHandler>();

        // Login / register (token hələ yoxdur)
        services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
        {
            client.BaseAddress = baseUri;
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IHotelService, HotelApiService>();
        services.AddScoped<IRoomService, RoomApiService>();
        services.AddScoped<IBookingService, BookingApiService>();
        services.AddScoped<IReviewService, ReviewApiService>();
        services.AddScoped<IAmenityService, AmenityApiService>();
        services.AddScoped<IHotelChainService, HotelChainApiService>();
        services.AddScoped<IPromoCodeService, PromoCodeApiService>();
        services.AddScoped<ICityGuideService, CityGuideApiService>();
        services.AddScoped<IWishlistService, WishlistApiService>();
        services.AddScoped<ICarRentalService, CarRentalApiService>();
        services.AddScoped<ITaxiService, TaxiApiService>();
        services.AddScoped<IFlightService, FlightApiService>();
        services.AddScoped<IAnalyticsService, AnalyticsApiService>();
        services.AddScoped<IProfileApiClient, ProfileApiClient>();
        services.AddScoped<IVoucherApiClient, VoucherApiClient>();
        services.AddScoped<IEmailLogService, EmailLogApiService>();
        services.AddScoped<ITripPackageService, TripApiService>();
        services.AddScoped<IWeatherService, WeatherApiService>();
        services.AddScoped<IExportApiClient, ExportApiClient>();

        return services;
    }
}
