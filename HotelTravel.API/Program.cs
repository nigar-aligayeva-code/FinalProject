using System.Text;
using System.Text.Json.Serialization;
using HotelTravel.API.Health;
using HotelTravel.API.Hubs;
using HotelTravel.API.Middleware;
using HotelTravel.API.Services;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Options;
using HotelTravel.Domain.Entities;
using HotelTravel.Infrastructure;
using HotelTravel.Infrastructure.Persistence;
using HotelTravel.Infrastructure.Services;
using HotelTravel.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace HotelTravel.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =========================================
            // CONTROLLERS + JSON
            // =========================================

            builder.Services
                .AddControllers(options =>
                {
                    // "string Name" sahəsi boş gələ bilər (məs. şəkilsiz otaq);
                    // yalnız [Required] olan sahələr məcburidir
                    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                })
                .AddJsonOptions(options =>
                {
                    // Enum-lar Swagger-də ad kimi görünsün ("Confirmed")
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // =========================================
            // DATABASE + SERVICES
            // =========================================

            builder.Services.AddHotelTravelInfrastructure(
                builder.Configuration.GetConnectionString("DefaultConnection")!);

            // EMAIL: "Email" bölməsi yoxdursa məktublar App_Data/Emails qovluğuna .eml kimi yazılır
            var emailOptions = builder.Configuration.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();

            if (string.IsNullOrWhiteSpace(emailOptions.PickupDirectory))
                emailOptions.PickupDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Emails");

            builder.Services.AddSingleton(emailOptions);

            // HAVA: Open-Meteo (pulsuz, açarsız) + 30 dəqiqəlik keş
            builder.Services.AddMemoryCache();

            // REAL REYSLƏR: "AviationStack:AccessKey" varsa admin real cədvəli idxal edə bilir
            builder.Services.AddSingleton(
                builder.Configuration.GetSection("AviationStack").Get<AviationStackOptions>() ?? new AviationStackOptions());
            builder.Services.AddHttpClient<IRealFlightScheduleProvider, AviationStackScheduleProvider>(client =>
                client.Timeout = TimeSpan.FromSeconds(15));

            // ÖDƏNİŞ: "Stripe:SecretKey" (sk_test_...) User Secrets-dədirsə onlayn ödəniş aktivdir
            builder.Services.AddSingleton(
                builder.Configuration.GetSection("Stripe").Get<StripeOptions>() ?? new StripeOptions());
            builder.Services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(8);
            });

            // VALYUTA: open.er-api.com (pulsuz, açarsız) + 6 saatlıq keş
            builder.Services.AddHttpClient<ICurrencyService, ExchangeRateService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(8);
            });

            // =========================================
            // IDENTITY (istifadəçilər və rollar)
            // =========================================

            builder.Services
                .AddIdentityCore<AppUser>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;
                    options.User.RequireUniqueEmail = true;

                    // 5 səhv cəhddən sonra hesab 5 dəqiqəlik bloklanır
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.AllowedForNewUsers = true;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // =========================================
            // JWT AUTHENTICATION
            // =========================================

            var jwtKey = builder.Configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey) ||
                Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key must be configured with at least 32 bytes.");
            }

            var jwtIssuer = builder.Configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("Jwt:Issuer is missing.");

            var jwtAudience = builder.Configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("Jwt:Audience is missing.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,

                        ValidateAudience = true,
                        ValidAudience = jwtAudience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,

                        RoleClaimType = System.Security.Claims.ClaimTypes.Role
                    };

                    // SignalR (WebSocket) başlıq göndərə bilmir: token query-də gəlir
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Query["access_token"];

                            if (!string.IsNullOrEmpty(token) &&
                                context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // =========================================
            // SIGNALR (canlı bildirişlər) + CORS
            // =========================================

            builder.Services.AddSignalR();

            // RATE LIMITING: login / qeydiyyat / şifrə bərpası üçün bir IP-dən dəqiqədə 10 sorğu
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsJsonAsync(
                        HotelTravel.Application.DTOs.Common.ApiResultDto.Fail(
                            "Too many attempts. Please wait a minute and try again."),
                        cancellationToken);
                };
            });

            // HEALTH CHECK: GET /health
            builder.Services
                .AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("database");
            builder.Services.AddSingleton<IRealtimePublisher, SignalRRealtimePublisher>();

            // Giriş və uçuş xatırlatmaları (fon xidməti, hər 10 dəqiqədən bir)
            builder.Services.AddHostedService<ReminderBackgroundService>();

            // Brauzer hub-a birbaşa qoşulur, ona görə MVC saytının ünvanına icazə lazımdır
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:5233", "https://localhost:7234" };

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("TripioWeb", policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });

            // =========================================
            // SWAGGER + AUTHORIZE
            // =========================================

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Tripio API",
                    Version = "v1",
                    Description =
                        "Hotel & Travel Reservation System REST API (ASP.NET Core 8, Clean Architecture).\n\n" +
                        "**How to try protected endpoints:**\n" +
                        "1. `POST /api/auth/login` with an account's email and password.\n" +
                        "2. Copy `accessToken` from the response.\n" +
                        "3. Click **Authorize** (top right) and paste the token.\n\n" +
                        "Endpoints marked *(admin)* need a token with the **Admin** role. " +
                        "Health check: `GET /health`. Live notifications: SignalR hub `/hubs/notifications`.",
                    Contact = new OpenApiContact
                    {
                        Name = "Tripio",
                        Url = new Uri("https://github.com/nigar-aligayeva-code/FinalProject")
                    }
                });

                // /// <summary> şərhləri endpoint və bölmə təsviri kimi göstər
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT access token."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            // =========================================
            // SEED DATA (API verilənlər bazasının sahibidir)
            // =========================================

            await SeedDatabaseAsync(app);

            // =========================================
            // HTTP PIPELINE
            // =========================================

            app.UseMiddleware<RequestTimingMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tripio API v1");
                options.DocumentTitle = "Tripio API";

                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // bölmələr bağlı açılsın
                options.EnableFilter();               // axtarış qutusu
                options.DisplayRequestDuration();     // sorğunun müddəti
                options.EnablePersistAuthorization(); // token səhifə yenilənəndə itməsin
            });

            // Development-də MVC API-yə http ilə müraciət edir (redirect token-i itirər)
            if (!app.Environment.IsDevelopment())
                app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("TripioWeb");

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotificationHub>("/hubs/notifications");

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = HealthResponseWriter.WriteAsync
            });

            app.Run();
        }

        private static async Task WaitForDatabaseAsync(AppDbContext context, ILogger logger)
        {
            const int maxAttempts = 10;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (await context.Database.CanConnectAsync())
                        return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Database not ready ({Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(3));
            }

            logger.LogError("Could not connect to the database. Check that SQL Server is running.");
        }

        // SQL Server konteyneri bir neçə saniyə gec açılır, ona görə bir neçə dəfə cəhd edirik
        private static async Task MigrateWithRetryAsync(AppDbContext context, ILogger logger)
        {
            const int maxAttempts = 20;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied.");
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    logger.LogWarning("Database not ready for migrations ({Attempt}/{Max}): {Message}",
                        attempt, maxAttempts, ex.Message);

                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }

        private static async Task SeedDatabaseAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var context = services.GetRequiredService<AppDbContext>();

            // Docker-də baza boşdur: "Database:MigrateOnStartup" = true olanda
            // migration-lar avtomatik tətbiq olunur (baza yoxdursa yaradılır).
            // Lokal işdə (Visual Studio) bu söndürülüdür, Update-Database ilə edilir.
            if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
                await MigrateWithRetryAsync(context, logger);
            else
                // SQL Server hazır olana qədər gözlə (kompüter yenicə açılıbsa gecikə bilər)
                await WaitForDatabaseAsync(context, logger);

            // Rollar + admin (AdminSettings yoxdursa admin addımı ötürülür)
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            if (string.IsNullOrWhiteSpace(configuration["AdminSettings:Email"]) ||
                string.IsNullOrWhiteSpace(configuration["AdminSettings:Password"]))
            {
                foreach (var role in new[] { "Admin", "User" })
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole(role));
                }

                logger.LogWarning(
                    "AdminSettings is not configured for the API: admin user seeding skipped.");
            }
            else
            {
                await IdentitySeeder.SeedAsync(roleManager, userManager, configuration);
            }

            await HotelDataSeeder.SeedAsync(context);

            // Hər otelin öz otaq dəsti (King, Penthouse, Villa...); baza otaqların bəziləri gizlədilir
            await RoomVarietySeeder.SeedAsync(context);

            // Şəkillər MVC-nin wwwroot qovluğundadır
            var webRoot = configuration["Media:WebRootPath"];

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.GetFullPath(Path.Combine(
                    app.Environment.ContentRootPath, "..", "HotelTravel.Web", "wwwroot"));
            }

            await MediaFixSeeder.SeedAsync(context, Directory.Exists(webRoot) ? webRoot : null);
            await CityGuideSeeder.SeedAsync(context);
            await RentalCarSeeder.SeedAsync(context);
            await FlightRouteSeeder.SeedAsync(context);

            // Otel şəhərləri arasında reysi olmayan istiqamətlər avtomatik yaradılır (admin əllə yaratmır)
            await services.GetRequiredService<IFlightNetworkService>().GenerateMissingAsync();
        }
    }
}
