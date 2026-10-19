using System.Text;
using System.Text.Json.Serialization;
using HotelTravel.API.Middleware;
using HotelTravel.Application.Options;
using HotelTravel.Domain.Entities;
using HotelTravel.Infrastructure;
using HotelTravel.Infrastructure.Persistence;
using HotelTravel.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
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
                });

            builder.Services.AddAuthorization();

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
                        "Hotel & Travel Reservation System REST API. " +
                        "Login via POST /api/auth/login, then click Authorize and paste the accessToken."
                });

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

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tripio API v1");
                options.DocumentTitle = "Tripio API";
            });

            // Development-də MVC API-yə http ilə müraciət edir (redirect token-i itirər)
            if (!app.Environment.IsDevelopment())
                app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

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

        private static async Task SeedDatabaseAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var context = services.GetRequiredService<AppDbContext>();

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
        }
    }
}
