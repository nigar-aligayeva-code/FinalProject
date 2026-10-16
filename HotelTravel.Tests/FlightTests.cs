using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class FlightTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<FlightRoute> SeedRouteAsync(
        AppDbContext context,
        int economySeats = 150,
        int businessSeats = 12)
    {
        var route = new FlightRoute
        {
            FlightNumber = "J2 8011",
            Airline = "Azerbaijan Airlines",
            FromCity = "Baku",
            FromCode = "GYD",
            ToCity = "Istanbul",
            ToCode = "IST",
            DepartureTime = new TimeSpan(8, 0, 0),
            DurationMinutes = 200,
            EconomyPrice = 260,
            BusinessPrice = 780,
            EconomySeats = economySeats,
            BusinessSeats = businessSeats,
            IsActive = true
        };

        context.FlightRoutes.Add(route);
        await context.SaveChangesAsync();

        return route;
    }

    private static CreateFlightBookingDto NewDto(int routeId, int passengers = 1) => new()
    {
        RouteId = routeId,
        FlightDate = DateTime.Today.AddDays(30),
        Cabin = CabinClass.Economy,
        Passengers = passengers,
        PassengerNames = Enumerable.Range(1, passengers).Select(i => $"Passenger {i}").ToList(),
        ContactName = "Test Guest",
        Email = "guest@test.com",
        Phone = "+994500000000"
    };


    // =========================
    // RULES
    // =========================
    [Fact]
    public void GetArrival_ShouldUseLocalTimeOfDestination()
    {
        var departure = new DateTime(2026, 11, 1, 8, 0, 0);

        // Bakı (UTC+4) -> İstanbul (UTC+3), 200 dəq: 08:00 + 3s20d - 1s = 10:20
        var arrival = FlightRules.GetArrival(departure, 200, "Baku", "Istanbul");

        Assert.Equal(new DateTime(2026, 11, 1, 10, 20, 0), arrival);
    }

    [Fact]
    public void GetArrival_CanBeNextDay()
    {
        var departure = new DateTime(2026, 11, 1, 23, 55, 0);

        var arrival = FlightRules.GetArrival(departure, 200, "Baku", "Dubai");

        Assert.Equal(new DateTime(2026, 11, 2, 3, 15, 0), arrival);
    }

    [Theory]
    [InlineData(30, 260)]   // erkən: baza qiymət
    [InlineData(10, 299)]   // 14 gündən az: +15% (299)
    [InlineData(1, 338)]    // 3 gündən az: +30%
    public void CalculateFare_ShouldRiseCloseToDeparture(int daysAhead, decimal expected)
    {
        var now = new DateTime(2026, 10, 15, 10, 0, 0);
        var departure = now.Date.AddDays(daysAhead).AddHours(8);

        Assert.Equal(expected, FlightRules.CalculateFare(260m, departure, now));
    }

    [Fact]
    public void ValidateSearch_ShouldRejectBadInput()
    {
        var today = new DateTime(2026, 10, 15);

        Assert.NotNull(FlightRules.ValidateSearch("Baku", "baku", today.AddDays(5), 1, today));
        Assert.NotNull(FlightRules.ValidateSearch("Baku", "Istanbul", today.AddDays(-1), 1, today));
        Assert.NotNull(FlightRules.ValidateSearch("Baku", "Istanbul", today.AddDays(5), 0, today));
        Assert.NotNull(FlightRules.ValidateSearch("Baku", "Istanbul", today.AddDays(FlightRules.MaxDaysAhead + 1), 1, today));
        Assert.Null(FlightRules.ValidateSearch("Baku", "Istanbul", today.AddDays(5), 2, today));
    }

    [Fact]
    public void CanCancel_ShouldAllowOnlyUntil24HoursBefore()
    {
        var now = new DateTime(2026, 10, 15, 10, 0, 0);

        Assert.True(FlightRules.CanCancel(FlightBookingStatus.Confirmed, now.AddHours(30), now));
        Assert.False(FlightRules.CanCancel(FlightBookingStatus.Confirmed, now.AddHours(20), now));
        Assert.False(FlightRules.CanCancel(FlightBookingStatus.Cancelled, now.AddDays(10), now));
    }

    [Theory]
    [InlineData(200, "3h 20m")]
    [InlineData(45, "45m")]
    [InlineData(120, "2h")]
    public void FormatDuration_ShouldBeReadable(int minutes, string expected)
    {
        Assert.Equal(expected, FlightRules.FormatDuration(minutes));
    }


    // =========================
    // SERVICE
    // =========================
    [Fact]
    public async Task BookAsync_ShouldSaveTicketWithServerPrice()
    {
        using var context = CreateContext();
        var route = await SeedRouteAsync(context);
        var service = new FlightService(context);

        var result = await service.BookAsync(NewDto(route.Id, 2), "user-1");

        Assert.True(result.Success);

        var saved = await context.FlightBookings.SingleAsync();

        Assert.StartsWith("FL-", saved.ConfirmationCode);
        Assert.Equal(260m, saved.FarePerPassenger);
        Assert.Equal(520m, saved.TotalPrice);
        Assert.Equal("Passenger 1\nPassenger 2", saved.PassengerNames);
        Assert.Equal(DateTime.Today.AddDays(30).AddHours(8), saved.DepartureTime);
    }

    [Fact]
    public async Task BookAsync_ShouldRequireEveryPassengerName()
    {
        using var context = CreateContext();
        var route = await SeedRouteAsync(context);
        var service = new FlightService(context);

        var dto = NewDto(route.Id, 2);
        dto.PassengerNames[1] = "  ";

        var result = await service.BookAsync(dto, null);

        Assert.False(result.Success);
        Assert.Empty(context.FlightBookings);
    }

    [Fact]
    public async Task BookAsync_ShouldNotOversellSeats()
    {
        using var context = CreateContext();
        var route = await SeedRouteAsync(context, economySeats: 3);
        var service = new FlightService(context);

        var first = await service.BookAsync(NewDto(route.Id, 2), null);
        var second = await service.BookAsync(NewDto(route.Id, 2), null);
        var third = await service.BookAsync(NewDto(route.Id, 1), null);

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.True(third.Success);
    }

    [Fact]
    public async Task BookAsync_ShouldRejectBusinessWhenRouteHasNone()
    {
        using var context = CreateContext();
        var route = await SeedRouteAsync(context, businessSeats: 0);
        var service = new FlightService(context);

        var dto = NewDto(route.Id);
        dto.Cabin = CabinClass.Business;

        var result = await service.BookAsync(dto, null);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SearchAsync_ShouldShowSeatsLeftAfterBooking()
    {
        using var context = CreateContext();
        var route = await SeedRouteAsync(context, economySeats: 10);
        var service = new FlightService(context);

        await service.BookAsync(NewDto(route.Id, 4), null);

        var results = await service.SearchAsync("baku", "Istanbul", DateTime.Today.AddDays(30), 1, CabinClass.Economy);

        var flight = Assert.Single(results);
        Assert.Equal(6, flight.SeatsLeft);
        Assert.True(flight.IsAvailable);
        Assert.Equal(new TimeSpan(10, 20, 0), flight.Arrival.TimeOfDay);
    }

    [Fact]
    public async Task CancelByUserAsync_ShouldFreeTheSeats()
    {
        using var context = CreateContext();
        var route = await SeedRouteAsync(context, economySeats: 2);
        var service = new FlightService(context);

        await service.BookAsync(NewDto(route.Id, 2), "owner");
        var booking = await context.FlightBookings.SingleAsync();

        var other = await service.CancelByUserAsync(booking.Id, "someone-else");
        Assert.False(other.Success);

        var own = await service.CancelByUserAsync(booking.Id, "owner");
        Assert.True(own.Success);

        var again = await service.BookAsync(NewDto(route.Id, 2), null);
        Assert.True(again.Success);
    }
}
