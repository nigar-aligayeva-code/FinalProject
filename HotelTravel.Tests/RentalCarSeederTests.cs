using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using HotelTravel.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class RentalCarSeederTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Hotel Hotel(string city) => new()
    {
        Name = city + " Hotel", Description = "d", Address = "a", City = city, Country = "c",
        Phone = "1", Email = "h@h.az", MainImage = "", StarRating = 4,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    private static RentalCar Car(string city, string model) => new()
    {
        City = city, Model = model, Category = CarCategory.Economy, Seats = 5, PricePerDay = 50,
        IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public void Fleets_ShouldDifferBetweenCountries()
    {
        var baku = RentalCarSeeder.FleetFor("Baku").Select(c => c.Model).ToHashSet();
        var dubai = RentalCarSeeder.FleetFor("Dubai").Select(c => c.Model).ToHashSet();
        var tbilisi = RentalCarSeeder.FleetFor("Tbilisi").Select(c => c.Model).ToHashSet();

        Assert.False(baku.SetEquals(dubai));
        Assert.False(baku.SetEquals(tbilisi));
        Assert.Contains("Nissan Patrol", dubai);
        Assert.Contains("Toyota Prius", tbilisi);
    }

    [Fact]
    public void UnknownCity_ShouldGetDefaultFleet()
    {
        Assert.NotEmpty(RentalCarSeeder.FleetFor("Paris"));
    }

    [Fact]
    public async Task Seed_ShouldReplaceOldIdenticalFleet_ButKeepAdminChanges()
    {
        await using var context = CreateContext();
        context.Hotels.AddRange(Hotel("Baku"), Hotel("Dubai"));

        // Bakı: köhnə eyni park -> yenilənməlidir
        foreach (var m in new[] { "Hyundai Accent", "Toyota Corolla", "Kia Sportage", "Mercedes-Benz V-Class" })
            context.RentalCars.Add(Car("Baku", m));

        // Dubay: admin öz maşınını əlavə edib -> toxunulmamalıdır
        context.RentalCars.Add(Car("Dubai", "Admin Custom Car"));
        await context.SaveChangesAsync();

        await RentalCarSeeder.SeedAsync(context);

        var bakuActive = await context.RentalCars.Where(c => c.City == "Baku" && !c.IsDeleted).ToListAsync();
        var bakuOld = await context.RentalCars.Where(c => c.City == "Baku" && c.IsDeleted).ToListAsync();
        var dubai = await context.RentalCars.Where(c => c.City == "Dubai" && !c.IsDeleted).ToListAsync();

        Assert.Contains(bakuActive, c => c.Model == "Toyota Land Cruiser Prado");
        Assert.Equal(4, bakuOld.Count);
        Assert.Single(dubai);
    }

    [Fact]
    public async Task Seed_ShouldBeIdempotent()
    {
        await using var context = CreateContext();
        context.Hotels.Add(Hotel("Batumi"));
        await context.SaveChangesAsync();

        await RentalCarSeeder.SeedAsync(context);
        var first = await context.RentalCars.CountAsync();

        await RentalCarSeeder.SeedAsync(context);

        Assert.Equal(first, await context.RentalCars.CountAsync());
    }
}
