using HotelTravel.Application.DTOs.Taxis;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class AirportTaxiTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<Hotel> SeedHotelAsync(AppDbContext context, string city = "Baku")
    {
        var hotel = new Hotel
        {
            Name = "Hotel " + city,
            Description = "Desc",
            Address = "Address",
            City = city,
            Country = "Country",
            Phone = "000",
            Email = "hotel@test.com",
            MainImage = "hotel.jpg",
            StarRating = 5
        };

        context.Hotels.Add(hotel);
        await context.SaveChangesAsync();

        return hotel;
    }

    private static CreateTaxiBookingDto NewDto(int hotelId) => new()
    {
        HotelId = hotelId,
        Direction = TaxiDirection.FromAirport,
        PickupTime = DateTime.Now.AddDays(2),
        Passengers = 2,
        Luggage = 2,
        VehicleType = TaxiVehicleType.Standard,
        FlightNumber = "j2 8012",
        PassengerName = "Test Guest",
        Email = "guest@test.com",
        Phone = "+994500000000"
    };


    // =========================
    // RULES
    // =========================
    [Theory]
    [InlineData(TaxiVehicleType.Standard, 25, 40)]    // 15 + 25 × 1.0
    [InlineData(TaxiVehicleType.Comfort, 25, 63)]     // 25 + 37.5 = 62.5 -> 63
    [InlineData(TaxiVehicleType.Minivan, 5, 44)]      // 35 + 9
    [InlineData(TaxiVehicleType.Executive, 0, 60)]
    public void CalculatePrice_ShouldUseBaseFareAndDistance(
        TaxiVehicleType type,
        int km,
        decimal expected)
    {
        Assert.Equal(expected, TaxiRules.CalculatePrice(type, km));
    }

    [Theory]
    [InlineData(TaxiVehicleType.Standard, 3, 2, true)]
    [InlineData(TaxiVehicleType.Standard, 4, 2, false)]
    [InlineData(TaxiVehicleType.Standard, 2, 3, false)]
    [InlineData(TaxiVehicleType.Minivan, 7, 7, true)]
    [InlineData(TaxiVehicleType.Minivan, 0, 0, false)]
    public void Fits_ShouldCheckPassengersAndLuggage(
        TaxiVehicleType type,
        int passengers,
        int luggage,
        bool expected)
    {
        Assert.Equal(expected, TaxiRules.Fits(type, passengers, luggage));
    }

    [Fact]
    public void ValidateRequest_ShouldRejectTooSoonPickup()
    {
        var now = new DateTime(2026, 10, 15, 10, 0, 0);

        Assert.NotNull(TaxiRules.ValidateRequest(now.AddHours(2), 2, 2, now));
        Assert.Null(TaxiRules.ValidateRequest(now.AddHours(3), 2, 2, now));
    }

    [Fact]
    public void ValidateRequest_ShouldRejectFarFutureAndBadCounts()
    {
        var now = new DateTime(2026, 10, 15, 10, 0, 0);

        Assert.NotNull(TaxiRules.ValidateRequest(now.AddDays(TaxiRules.MaxDaysAhead + 1), 2, 2, now));
        Assert.NotNull(TaxiRules.ValidateRequest(now.AddDays(1), 0, 2, now));
        Assert.NotNull(TaxiRules.ValidateRequest(now.AddDays(1), 2, 99, now));
    }

    [Fact]
    public void CanCancel_ShouldAllowOnlyUntil24HoursBefore()
    {
        var now = new DateTime(2026, 10, 15, 10, 0, 0);

        Assert.True(TaxiRules.CanCancel(TaxiBookingStatus.Confirmed, now.AddHours(25), now));
        Assert.False(TaxiRules.CanCancel(TaxiBookingStatus.Confirmed, now.AddHours(23), now));
        Assert.False(TaxiRules.CanCancel(TaxiBookingStatus.Cancelled, now.AddDays(5), now));
    }

    [Fact]
    public void GetAirport_ShouldIgnoreCase()
    {
        Assert.Equal("GYD", TaxiRules.GetAirport("baku")?.Code);
        Assert.Null(TaxiRules.GetAirport("Atlantis"));
    }


    // =========================
    // SERVICE
    // =========================
    [Fact]
    public async Task BookAsync_ShouldSaveWithServerPrice()
    {
        using var context = CreateContext();
        var hotel = await SeedHotelAsync(context);
        var service = new TaxiService(context);

        var result = await service.BookAsync(NewDto(hotel.Id), "user-1");

        Assert.True(result.Success);

        var saved = await context.TaxiBookings.SingleAsync();

        Assert.Equal("GYD", saved.AirportCode);
        Assert.Equal(40m, saved.Price);
        Assert.Equal("J2 8012", saved.FlightNumber);
        Assert.Equal(TaxiBookingStatus.Confirmed, saved.Status);
        Assert.StartsWith("TX-", saved.ConfirmationCode);
    }

    [Fact]
    public async Task BookAsync_ShouldRejectVehicleThatIsTooSmall()
    {
        using var context = CreateContext();
        var hotel = await SeedHotelAsync(context);
        var service = new TaxiService(context);

        var dto = NewDto(hotel.Id);
        dto.Passengers = 5;

        var result = await service.BookAsync(dto, null);

        Assert.False(result.Success);
        Assert.Empty(context.TaxiBookings);
    }

    [Fact]
    public async Task BookAsync_ShouldRejectCityWithoutAirport()
    {
        using var context = CreateContext();
        var hotel = await SeedHotelAsync(context, "Atlantis");
        var service = new TaxiService(context);

        var result = await service.BookAsync(NewDto(hotel.Id), null);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CancelByUserAsync_ShouldOnlyCancelOwnBooking()
    {
        using var context = CreateContext();
        var hotel = await SeedHotelAsync(context);
        var service = new TaxiService(context);

        await service.BookAsync(NewDto(hotel.Id), "owner");
        var booking = await context.TaxiBookings.SingleAsync();

        var other = await service.CancelByUserAsync(booking.Id, "someone-else");
        Assert.False(other.Success);

        var own = await service.CancelByUserAsync(booking.Id, "owner");
        Assert.True(own.Success);
        Assert.Equal(TaxiBookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public void GetOptions_ShouldMarkVehiclesThatFit()
    {
        using var context = CreateContext();
        var service = new TaxiService(context);

        var options = service.GetOptions(25, 5, 3);

        Assert.Equal(4, options.Count);
        Assert.Single(options, o => o.Fits);
        Assert.Equal(TaxiVehicleType.Minivan, options.Single(o => o.Fits).Type);
    }
}
