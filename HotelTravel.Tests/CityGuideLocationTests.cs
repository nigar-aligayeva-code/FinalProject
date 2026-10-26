using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using HotelTravel.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

// Şəhər bələdçisi: yerlərin xəritə koordinatları
public class CityGuideLocationTests
{
    private static AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Seed_ShouldGiveEveryPlaceACoordinate()
    {
        using var context = CreateContext();

        await CityGuideSeeder.SeedAsync(context);

        var places = await context.CityPlaces.ToListAsync();

        Assert.NotEmpty(places);
        Assert.All(places, p =>
        {
            Assert.NotNull(p.Latitude);
            Assert.NotNull(p.Longitude);
            Assert.InRange(p.Latitude!.Value, -90, 90);
            Assert.InRange(p.Longitude!.Value, -180, 180);
        });
    }

    [Fact]
    public async Task FillMissing_ShouldAddCoordinates_ToOldRows_AndKeepAdminValues()
    {
        using var context = CreateContext();

        context.CityPlaces.AddRange(
            new CityPlace { City = "Istanbul", Name = "Hagia Sophia", Category = PlaceCategory.Sight, Description = "d" },
            new CityPlace { City = "Baku", Name = "Maiden Tower", Category = PlaceCategory.Sight, Description = "d", Latitude = 40.3661, Longitude = 49.8371 },
            new CityPlace { City = "Baku", Name = "Unknown cafe", Category = PlaceCategory.Food, Description = "d" });
        await context.SaveChangesAsync();

        // Cədvəl boş deyil -> yalnız çatışmayan koordinatlar doldurulur
        await CityGuideSeeder.SeedAsync(context);

        var sophia = await context.CityPlaces.SingleAsync(p => p.Name == "Hagia Sophia");
        var maiden = await context.CityPlaces.SingleAsync(p => p.Name == "Maiden Tower");
        var unknown = await context.CityPlaces.SingleAsync(p => p.Name == "Unknown cafe");

        Assert.Equal(41.0086, sophia.Latitude);
        Assert.Equal(40.3661, maiden.Latitude);   // admin yazıb - toxunulmur
        Assert.Null(unknown.Latitude);            // tanınmır - boş qalır
        Assert.Equal(3, await context.CityPlaces.CountAsync());
    }
}
