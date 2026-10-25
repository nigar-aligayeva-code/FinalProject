using System.Text.Json;
using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Infrastructure.Persistence;
using HotelTravel.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class FlightNetworkTests
{
    private static readonly string[] Cities = { "Baku", "Gabala", "Istanbul", "Antalya", "Tbilisi", "Batumi", "Dubai" };

    private sealed class FakeProvider : IRealFlightScheduleProvider
    {
        public bool IsEnabled { get; init; } = true;
        public int Calls { get; private set; }
        public Dictionary<string, List<RealFlight>> Data { get; } = new();

        public Task<RealScheduleResult> GetDeparturesAsync(string fromIata, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(new RealScheduleResult(true, null,
                Data.TryGetValue(fromIata, out var list) ? list : new List<RealFlight>()));
        }
    }

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task SeedHotelsAsync(AppDbContext context, params string[] cities)
    {
        foreach (var city in cities)
        {
            context.Hotels.Add(new Hotel
            {
                Name = city + " Hotel", Description = "d", Address = "a", City = city, Country = "c",
                Phone = "1", Email = "h@h.az", MainImage = "", StarRating = 4
            });
        }

        await context.SaveChangesAsync();
    }

    // =========================
    // PLANLAYICI (təmiz qaydalar)
    // =========================

    [Fact]
    public void Plan_ShouldConnectEveryBigCityPair_InBothDirections()
    {
        var routes = FlightNetworkPlanner.Plan(Cities);
        var pairs = routes.Select(r => (r.FromCity, r.ToCity)).ToHashSet();

        var big = Cities.Where(c => c != "Gabala").ToArray();
        foreach (var from in big)
            foreach (var to in big.Where(t => t != from))
                Assert.Contains((from, to), pairs);
    }

    [Fact]
    public void Plan_SmallAirport_ShouldOnlyFlyToHubs()
    {
        var gabala = FlightNetworkPlanner.Plan(Cities).Where(r => r.FromCity == "Gabala").ToList();

        Assert.NotEmpty(gabala);
        Assert.All(gabala, r => Assert.Contains(r.ToCity, new[] { "Baku", "Istanbul" }));
    }

    [Fact]
    public void Plan_ShouldBeDeterministic_AndUseUniqueFlightNumbers()
    {
        var first = FlightNetworkPlanner.Plan(Cities);
        var second = FlightNetworkPlanner.Plan(Cities);

        Assert.Equal(first, second);
        Assert.Equal(first.Count, first.Select(r => r.FlightNumber).Distinct().Count());
    }

    [Fact]
    public void Plan_ShouldUseLocalCarriers()
    {
        var routes = FlightNetworkPlanner.Plan(Cities);

        Assert.Contains(routes, r => r.FromCity == "Baku" && r.ToCity == "Istanbul" && r.Airline == "Azerbaijan Airlines");
        Assert.Contains(routes, r => r.FromCity == "Baku" && r.ToCity == "Istanbul" && r.Airline == "Turkish Airlines");
        Assert.Contains(routes, r => r.FromCity == "Dubai" && r.Airline == "flydubai");
    }

    [Fact]
    public void Distance_DurationAndPrice_ShouldBeRealistic()
    {
        var km = FlightNetworkPlanner.DistanceKm("Baku", "Istanbul")!.Value;

        Assert.InRange(km, 1700, 1850);                                   // real: ~1780 km
        Assert.InRange(FlightNetworkPlanner.DurationFor(km), 170, 210);   // ~3 saat
        Assert.True(FlightNetworkPlanner.EconomyFareFor(km) > FlightNetworkPlanner.EconomyFareFor(450));
        Assert.Equal(0m, FlightNetworkPlanner.BusinessFareFor(300));      // qısa reysdə Business yoxdur
        Assert.Null(FlightNetworkPlanner.DistanceKm("Baku", "Paris"));
    }

    // =========================
    // SERVİS (InMemory baza)
    // =========================

    [Fact]
    public async Task GenerateMissing_ShouldAddOnlyMissingPairs_AndBeIdempotent()
    {
        await using var context = CreateContext();
        await SeedHotelsAsync(context, "Baku", "Tbilisi", "Dubai");

        // Admin artıq Bakı -> Dubay reysi yaradıb: ona toxunulmamalıdır
        context.FlightRoutes.Add(new FlightRoute
        {
            FlightNumber = "J2 9999", Airline = "Azerbaijan Airlines", FromCity = "Baku", FromCode = "GYD",
            ToCity = "Dubai", ToCode = "DXB", DepartureTime = new TimeSpan(9, 0, 0), DurationMinutes = 190,
            EconomyPrice = 300, EconomySeats = 150, IsActive = true
        });
        await context.SaveChangesAsync();

        var service = new FlightNetworkService(context, new FakeProvider { IsEnabled = false });

        var first = await service.GenerateMissingAsync();
        var second = await service.GenerateMissingAsync();

        Assert.True(first.Added > 0);
        Assert.Equal(0, second.Added);
        Assert.Single(await context.FlightRoutes.Where(r => r.FromCity == "Baku" && r.ToCity == "Dubai").ToListAsync());
        Assert.Contains(await context.FlightRoutes.ToListAsync(), r => r.FromCity == "Tbilisi" && r.ToCity == "Dubai");
    }

    [Fact]
    public async Task ImportReal_ShouldAddFlightsBetweenHotelCities_OneRequestPerAirport()
    {
        await using var context = CreateContext();
        await SeedHotelsAsync(context, "Baku", "Istanbul");

        var provider = new FakeProvider();
        provider.Data["GYD"] = new List<RealFlight>
        {
            new("J2 8011", "Azerbaijan Airlines", "GYD", "IST", new TimeSpan(8, 0, 0), new TimeSpan(10, 20, 0)),
            new("J2 8001", "Azerbaijan Airlines", "GYD", "LHR", new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0)) // otel şəhəri deyil
        };

        var service = new FlightNetworkService(context, provider);
        var result = await service.ImportRealAsync();

        Assert.True(result.Success);
        Assert.Equal(1, result.Added);
        Assert.Equal(2, provider.Calls);

        var route = await context.FlightRoutes.SingleAsync();
        Assert.Equal("J2 8011", route.FlightNumber);
        Assert.Equal(200, route.DurationMinutes); // 08:00 (+4) -> 10:20 (+3) = 3 saat 20 dəq
    }

    [Fact]
    public async Task ImportReal_ShouldFail_WhenNotConfigured()
    {
        await using var context = CreateContext();
        var service = new FlightNetworkService(context, new FakeProvider { IsEnabled = false });

        Assert.False((await service.ImportRealAsync()).Success);
    }

    // =========================
    // AVIATIONSTACK CAVABI
    // =========================

    [Fact]
    public void AviationStack_Parse_ShouldReadFlights_AndSkipCodeshares()
    {
        const string json = """
        {
          "data": [
            {
              "departure": { "iata": "GYD", "scheduled": "2026-10-25T08:05:00+00:00" },
              "arrival":   { "iata": "IST", "scheduled": "2026-10-25T10:25:00+00:00" },
              "airline":   { "name": "Azerbaijan Airlines", "iata": "J2" },
              "flight":    { "iata": "J28011", "codeshared": null }
            },
            {
              "departure": { "iata": "GYD", "scheduled": "2026-10-25T08:05:00+00:00" },
              "arrival":   { "iata": "IST", "scheduled": "2026-10-25T10:25:00+00:00" },
              "airline":   { "name": "Turkish Airlines", "iata": "TK" },
              "flight":    { "iata": "TK9311", "codeshared": { "flight_iata": "j28011" } }
            }
          ]
        }
        """;

        var result = AviationStackScheduleProvider.Parse(JsonDocument.Parse(json).RootElement, "GYD");

        Assert.True(result.Success);
        var flight = Assert.Single(result.Flights);
        Assert.Equal("J2 8011", flight.FlightNumber);
        Assert.Equal(new TimeSpan(8, 5, 0), flight.DepartureLocal);
        Assert.Equal("IST", flight.ToCode);
    }

    [Fact]
    public void AviationStack_Parse_ShouldReturnApiError()
    {
        var json = """{ "error": { "code": "invalid_access_key", "message": "You have not supplied a valid API Access Key." } }""";

        var result = AviationStackScheduleProvider.Parse(JsonDocument.Parse(json).RootElement, "GYD");

        Assert.False(result.Success);
        Assert.Contains("valid API Access Key", result.Error);
    }

    [Theory]
    [InlineData("J28011", "J2", "J2 8011")]
    [InlineData("TK333", "TK", "TK 333")]
    [InlineData("FZ712", null, "FZ 712")]
    public void FormatNumber_ShouldAddSpaceAfterAirlineCode(string iata, string? airline, string expected)
    {
        Assert.Equal(expected, AviationStackScheduleProvider.FormatNumber(iata, airline));
    }
}
