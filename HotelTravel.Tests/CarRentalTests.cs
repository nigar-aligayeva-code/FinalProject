using HotelTravel.Application.DTOs.Cars;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class CarRentalTests
{
    private static readonly DateTime Today = DateTime.Today;

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<RentalCar> SeedCarAsync(AppDbContext context)
    {
        var car = new RentalCar
        {
            City = "Baku",
            Model = "Toyota Corolla",
            Category = CarCategory.Sedan,
            Seats = 5,
            PricePerDay = 75,
            IsActive = true
        };

        context.RentalCars.Add(car);
        await context.SaveChangesAsync();

        return car;
    }

    private static CreateCarRentalDto Request(int carId, int fromDays, int toDays) => new()
    {
        CarId = carId,
        PickupDate = Today.AddDays(fromDays),
        ReturnDate = Today.AddDays(toDays),
        DriverName = "Nigar Aliyeva",
        Email = "nigar@test.com",
        Phone = "+994000000000",
        HasLicence = true
    };


    // =========================
    // RULES
    // =========================
    [Fact]
    public void ValidateDates_ShouldRejectPastAndWrongOrder()
    {
        Assert.NotNull(CarRentalRules.ValidateDates(Today.AddDays(-1), Today.AddDays(2), Today));
        Assert.NotNull(CarRentalRules.ValidateDates(Today.AddDays(3), Today.AddDays(3), Today));
        Assert.NotNull(CarRentalRules.ValidateDates(Today.AddDays(1), Today.AddDays(40), Today));
        Assert.Null(CarRentalRules.ValidateDates(Today.AddDays(1), Today.AddDays(4), Today));
    }

    [Fact]
    public void Overlaps_ShouldAllowSameDayHandover()
    {
        // 1-3 və 3-5: qaytarma günü yeni götürmə ola bilər
        Assert.False(CarRentalRules.Overlaps(
            Today.AddDays(1), Today.AddDays(3),
            Today.AddDays(3), Today.AddDays(5)));

        Assert.True(CarRentalRules.Overlaps(
            Today.AddDays(1), Today.AddDays(4),
            Today.AddDays(3), Today.AddDays(5)));
    }

    [Fact]
    public void CanCancel_OnlyBeforePickupDay()
    {
        Assert.True(CarRentalRules.CanCancel(CarRentalStatus.Confirmed, Today.AddDays(1), Today));
        Assert.False(CarRentalRules.CanCancel(CarRentalStatus.Confirmed, Today, Today));
        Assert.False(CarRentalRules.CanCancel(CarRentalStatus.Cancelled, Today.AddDays(5), Today));
    }


    // =========================
    // SERVICE
    // =========================
    [Fact]
    public async Task Book_ShouldCalculateTotal_AndCreateCode()
    {
        using var context = CreateContext();
        var car = await SeedCarAsync(context);
        var service = new CarRentalService(context);

        var result = await service.BookAsync(Request(car.Id, 1, 4), null);

        Assert.True(result.Success);
        Assert.StartsWith("CR-", result.ConfirmationCode);

        var rental = await context.CarRentals.SingleAsync();
        Assert.Equal(3, rental.Days);
        Assert.Equal(225m, rental.TotalPrice);
    }

    [Fact]
    public async Task Book_ShouldRejectOverlappingRental()
    {
        using var context = CreateContext();
        var car = await SeedCarAsync(context);
        var service = new CarRentalService(context);

        var first = await service.BookAsync(Request(car.Id, 1, 4), null);
        var overlap = await service.BookAsync(Request(car.Id, 3, 6), null);
        var afterReturn = await service.BookAsync(Request(car.Id, 4, 6), null);

        Assert.True(first.Success);
        Assert.False(overlap.Success);
        Assert.True(afterReturn.Success);
    }

    [Fact]
    public async Task Search_ShouldMarkBookedCarAsUnavailable()
    {
        using var context = CreateContext();
        var car = await SeedCarAsync(context);
        var service = new CarRentalService(context);

        await service.BookAsync(Request(car.Id, 2, 5), null);

        var cars = await service.SearchAsync("baku", Today.AddDays(3), Today.AddDays(4));

        Assert.Single(cars);
        Assert.False(cars[0].IsAvailable);
    }

    [Fact]
    public async Task CancelByUser_ShouldOnlyCancelOwnRental()
    {
        using var context = CreateContext();
        var car = await SeedCarAsync(context);
        var service = new CarRentalService(context);

        await service.BookAsync(Request(car.Id, 2, 4), "user-1");
        var rental = await context.CarRentals.SingleAsync();

        var other = await service.CancelByUserAsync(rental.Id, "user-2");
        var mine = await service.CancelByUserAsync(rental.Id, "user-1");

        Assert.False(other.Success);
        Assert.True(mine.Success);
        Assert.Equal(CarRentalStatus.Cancelled, (await context.CarRentals.SingleAsync()).Status);
    }
}
