using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

/// <summary>
/// Reyslər admin tərəfindən tək-tək yaradılmır:
///   • GenerateMissingAsync — otel şəhərləri arasında hələ reysi olmayan hər istiqamət üçün
///     FlightNetworkPlanner-in planını bazaya yazır (API hər açılanda da işləyir);
///   • ImportRealAsync — AviationStack-dan real reys nömrələri, şirkətlər və saatlar gətirilir.
///     Hər hava limanı üçün BİR sorğu (7 şəhər = 7 sorğu), çünki pulsuz plan ayda 100 sorğudur.
/// Mövcud (adminin dəyişdiyi) reyslərə toxunulmur.
/// </summary>
public class FlightNetworkService : IFlightNetworkService
{
    private readonly IAppDbContext _context;
    private readonly IRealFlightScheduleProvider _provider;

    public FlightNetworkService(IAppDbContext context, IRealFlightScheduleProvider provider)
    {
        _context = context;
        _provider = provider;
    }

    public Task<bool> IsRealDataEnabledAsync() => Task.FromResult(_provider.IsEnabled);

    public async Task<FlightNetworkResultDto> GenerateMissingAsync(CancellationToken cancellationToken = default)
    {
        var cities = await HotelCitiesAsync(cancellationToken);

        // Əvvəl yaradılmış çox qısa reysləri (məs. Bakı–Qəbələ) dayandırırıq.
        // Silmirik: köhnə biletlərin tarixçəsi qalsın.
        var disabled = await DisableShortRoutesAsync(cancellationToken);

        var existingPairs = (await _context.FlightRoutes
                .Where(r => !r.IsDeleted)
                .Select(r => new { r.FromCity, r.ToCity })
                .ToListAsync(cancellationToken))
            .Select(r => Pair(r.FromCity, r.ToCity))
            .ToHashSet();

        var existingNumbers = await ExistingNumbersAsync(cancellationToken);

        var toAdd = FlightNetworkPlanner.Plan(cities)
            .Where(p => !existingPairs.Contains(Pair(p.FromCity, p.ToCity)))
            .Where(p => !existingNumbers.Contains(p.FlightNumber))
            .Select(ToEntity)
            .ToList();

        if (toAdd.Count > 0)
        {
            await _context.FlightRoutes.AddRangeAsync(toAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new FlightNetworkResultDto
        {
            Success = true,
            Added = toAdd.Count,
            Message = toAdd.Count == 0 && disabled > 0
                ? $"{disabled} short flights were turned off (travel by car or taxi instead)."
                : toAdd.Count == 0
                ? "All hotel cities are already connected."
                : $"{toAdd.Count} new daily flights were created automatically."
        };
    }

    public async Task<FlightNetworkResultDto> ImportRealAsync(CancellationToken cancellationToken = default)
    {
        if (!_provider.IsEnabled)
        {
            return new FlightNetworkResultDto
            {
                Success = false,
                Message = "Real flight data is not configured. Add an AviationStack access key."
            };
        }

        var cities = await HotelCitiesAsync(cancellationToken);

        // Yalnız otel şəhərlərinin hava limanları
        var codeToCity = cities
            .Where(FlightNetworkPlanner.IsSupported)
            .Select(c => (City: c, Code: FlightRules.AirportCode(c)))
            .Where(x => x.Code.Length > 0)
            .ToDictionary(x => x.Code, x => x.City, StringComparer.OrdinalIgnoreCase);

        var existingNumbers = await ExistingNumbersAsync(cancellationToken);
        var added = new List<FlightRoute>();
        string? lastError = null;

        foreach (var (fromCode, fromCity) in codeToCity)
        {
            var result = await _provider.GetDeparturesAsync(fromCode, cancellationToken);

            if (!result.Success)
            {
                lastError = result.Error;
                continue;
            }

            foreach (var flight in result.Flights)
            {
                if (!codeToCity.TryGetValue(flight.ToCode, out var toCity) || toCity == fromCity)
                    continue;

                if (FlightNetworkPlanner.IsTooShortForFlight(fromCity, toCity))
                    continue;

                if (!existingNumbers.Add(flight.FlightNumber))
                    continue;

                var distance = FlightNetworkPlanner.DistanceKm(fromCity, toCity) ?? 0;

                added.Add(new FlightRoute
                {
                    FlightNumber = flight.FlightNumber,
                    Airline = flight.Airline,
                    FromCity = fromCity,
                    FromCode = fromCode.ToUpperInvariant(),
                    ToCity = toCity,
                    ToCode = flight.ToCode.ToUpperInvariant(),
                    DepartureTime = flight.DepartureLocal,
                    DurationMinutes = RealDuration(flight, fromCity, toCity, distance),
                    EconomyPrice = FlightNetworkPlanner.EconomyFareFor(distance),
                    BusinessPrice = FlightNetworkPlanner.BusinessFareFor(distance),
                    EconomySeats = 160,
                    BusinessSeats = FlightNetworkPlanner.BusinessFareFor(distance) > 0 ? 12 : 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        if (added.Count > 0)
        {
            await _context.FlightRoutes.AddRangeAsync(added, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (added.Count == 0 && lastError is not null)
            return new FlightNetworkResultDto { Success = false, Message = lastError };

        return new FlightNetworkResultDto
        {
            Success = true,
            Added = added.Count,
            Message = added.Count == 0
                ? "No new real flights were found between hotel cities today."
                : $"{added.Count} real flights were imported."
        };
    }

    /// <summary>
    /// Real müddət: hər iki saat yerli vaxtdır, UTC fərqi çıxılır (məs. Bakı +4, İstanbul +3).
    /// Nəticə ağlabatan deyilsə məsafədən hesablanır.
    /// </summary>
    public static int RealDuration(RealFlight flight, string fromCity, string toCity, int distanceKm)
    {
        var departureUtc = flight.DepartureLocal - TimeSpan.FromHours(FlightRules.UtcOffset(fromCity));
        var arrivalUtc = flight.ArrivalLocal - TimeSpan.FromHours(FlightRules.UtcOffset(toCity));

        var minutes = (int)(arrivalUtc - departureUtc).TotalMinutes;

        if (minutes < 0)
            minutes += 24 * 60; // gecə yarısından sonra enir

        return minutes is >= 20 and <= 900
            ? minutes
            : FlightNetworkPlanner.DurationFor(distanceKm);
    }

    /// <summary>Aktiv qısa reysləri passiv edir, neçəsinin dəyişdiyini qaytarır.</summary>
    private async Task<int> DisableShortRoutesAsync(CancellationToken cancellationToken)
    {
        var active = await _context.FlightRoutes
            .Where(r => !r.IsDeleted && r.IsActive)
            .ToListAsync(cancellationToken);

        var shortRoutes = active
            .Where(r => FlightNetworkPlanner.IsTooShortForFlight(r.FromCity, r.ToCity))
            .ToList();

        foreach (var route in shortRoutes)
        {
            route.IsActive = false;
            route.UpdatedAt = DateTime.UtcNow;
        }

        if (shortRoutes.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return shortRoutes.Count;
    }

    private async Task<List<string>> HotelCitiesAsync(CancellationToken cancellationToken) =>
        await _context.Hotels
            .Where(h => !h.IsDeleted)
            .Select(h => h.City)
            .Distinct()
            .ToListAsync(cancellationToken);

    private async Task<HashSet<string>> ExistingNumbersAsync(CancellationToken cancellationToken) =>
        (await _context.FlightRoutes
            .Where(r => !r.IsDeleted)
            .Select(r => r.FlightNumber)
            .ToListAsync(cancellationToken))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string Pair(string from, string to) => $"{from.ToLowerInvariant()}>{to.ToLowerInvariant()}";

    private static FlightRoute ToEntity(PlannedRoute p) => new()
    {
        FlightNumber = p.FlightNumber,
        Airline = p.Airline,
        FromCity = p.FromCity,
        FromCode = p.FromCode,
        ToCity = p.ToCity,
        ToCode = p.ToCode,
        DepartureTime = p.Departure,
        DurationMinutes = p.DurationMinutes,
        EconomyPrice = p.EconomyPrice,
        BusinessPrice = p.BusinessPrice,
        EconomySeats = p.EconomySeats,
        BusinessSeats = p.BusinessSeats,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
