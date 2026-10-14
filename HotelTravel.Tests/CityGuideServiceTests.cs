using HotelTravel.Application.DTOs.CityGuide;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class CityGuideServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static CityPlace Place(string city, string name, PlaceCategory category, bool deleted = false) => new()
    {
        City = city,
        Name = name,
        Category = category,
        Description = "Description text",
        IsDeleted = deleted
    };


    [Fact]
    public async Task GetByCity_ShouldIgnoreCase_AndSkipDeleted()
    {
        using var context = CreateContext();
        context.CityPlaces.AddRange(
            Place("Baku", "Old City", PlaceCategory.Sight),
            Place("Baku", "Closed place", PlaceCategory.Food, deleted: true),
            Place("Tbilisi", "Narikala", PlaceCategory.Sight));
        await context.SaveChangesAsync();

        var service = new CityGuideService(context);

        var places = await service.GetByCityAsync(" baku ");

        Assert.Single(places);
        Assert.Equal("Old City", places[0].Name);
    }

    [Fact]
    public async Task GetCities_ShouldReturnDistinctSortedCities()
    {
        using var context = CreateContext();
        context.CityPlaces.AddRange(
            Place("Tbilisi", "A", PlaceCategory.Sight),
            Place("Baku", "B", PlaceCategory.Sight),
            Place("Baku", "C", PlaceCategory.Tour));
        await context.SaveChangesAsync();

        var cities = await new CityGuideService(context).GetCitiesAsync();

        Assert.Equal(new[] { "Baku", "Tbilisi" }, cities);
    }

    [Fact]
    public async Task Save_ShouldCreate_ThenUpdate()
    {
        using var context = CreateContext();
        var service = new CityGuideService(context);

        await service.SaveAsync(new SaveCityPlaceDto
        {
            City = "Batumi",
            Name = "Boulevard",
            Category = PlaceCategory.Sight,
            Description = "Seaside walk"
        });

        var created = await context.CityPlaces.SingleAsync();

        await service.SaveAsync(new SaveCityPlaceDto
        {
            Id = created.Id,
            City = "Batumi",
            Name = "Batumi Boulevard",
            Category = PlaceCategory.Sight,
            Description = "Seaside walk",
            PriceFrom = null
        });

        var updated = await context.CityPlaces.SingleAsync();

        Assert.Equal("Batumi Boulevard", updated.Name);
    }

    [Fact]
    public async Task Delete_ShouldHidePlace()
    {
        using var context = CreateContext();
        var place = Place("Baku", "Old City", PlaceCategory.Sight);
        context.CityPlaces.Add(place);
        await context.SaveChangesAsync();

        var service = new CityGuideService(context);

        Assert.True(await service.DeleteAsync(place.Id));
        Assert.Empty(await service.GetByCityAsync("Baku"));
    }
}
