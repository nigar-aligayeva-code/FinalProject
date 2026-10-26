using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Services;

// Aviabilet qaydaları (unit test üçün ayrıca)
public static class FlightRules
{
    public const int MaxPassengers = 6;

    // Ən çox 330 gün qabağa
    public const int MaxDaysAhead = 330;

    // Uçuşa 2 saatdan az qalıbsa bilet satılmır
    public const int MinHoursBeforeDeparture = 2;

    // Pulsuz ləğv: uçuşdan 72 saat əvvələ qədər (sonra cərimə, bax CancellationPolicy)
    public const int FreeCancelHours = CancellationPolicy.FreeHours;

    // Şəhərlərin UTC saat qurşağı (gəliş vaxtı yerli vaxtla göstərilsin)
    private static readonly Dictionary<string, int> UtcOffsets =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Baku"] = 4,
            ["Gabala"] = 4,
            ["Tbilisi"] = 4,
            ["Batumi"] = 4,
            ["Dubai"] = 4,
            ["Istanbul"] = 3,
            ["Antalya"] = 3
        };

    public static int UtcOffset(string? city)
        => city != null && UtcOffsets.TryGetValue(city.Trim(), out var offset) ? offset : 4;

    // Hava limanı kodu və adı (TaxiRules-dakı siyahıdan)
    public static string AirportCode(string? city)
        => TaxiRules.GetAirport(city)?.Code ?? string.Empty;

    public static string AirportName(string? city)
        => TaxiRules.GetAirport(city)?.Name ?? $"{city} Airport";

    public static bool HasAirport(string? city)
        => TaxiRules.GetAirport(city) != null;

    // Uçuş tarixi + gündəlik saat
    public static DateTime GetDeparture(DateTime flightDate, TimeSpan departureTime)
        => flightDate.Date.Add(departureTime);

    // Gəliş (təyinat şəhərinin yerli vaxtı)
    public static DateTime GetArrival(
        DateTime departure,
        int durationMinutes,
        string fromCity,
        string toCity)
        => departure
            .AddMinutes(durationMinutes)
            .AddHours(UtcOffset(toCity) - UtcOffset(fromCity));

    // Son anda alınan bilet bahadır: 3 gündən az +30%, 14 gündən az +15%
    public static decimal CalculateFare(decimal basePrice, DateTime departure, DateTime now)
    {
        var daysAhead = (departure.Date - now.Date).Days;

        var factor = daysAhead < 3 ? 1.30m
            : daysAhead < 14 ? 1.15m
            : 1.00m;

        return Math.Round(basePrice * factor, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateTotal(decimal farePerPassenger, int passengers)
        => farePerPassenger * passengers;

    public static string? ValidateSearch(
        string? fromCity,
        string? toCity,
        DateTime date,
        int passengers,
        DateTime today)
    {
        if (string.IsNullOrWhiteSpace(fromCity) || string.IsNullOrWhiteSpace(toCity))
            return "Please choose where you fly from and to.";

        if (string.Equals(fromCity.Trim(), toCity.Trim(), StringComparison.OrdinalIgnoreCase))
            return "Departure and destination cannot be the same city.";

        if (passengers < 1 || passengers > MaxPassengers)
            return $"Passengers must be between 1 and {MaxPassengers}.";

        if (date.Date < today.Date)
            return "Departure date cannot be in the past.";

        if (date.Date > today.Date.AddDays(MaxDaysAhead))
            return $"Flights can be booked up to {MaxDaysAhead} days ahead.";

        return null;
    }

    // Bu uçuşa hələ bilet almaq olar?
    public static bool CanBook(DateTime departure, DateTime now)
        => departure >= now.AddHours(MinHoursBeforeDeparture);

    // Ləğv etmək olarmı? (pulsuz və ya cərimə ilə - son 3 saatda olmur)
    public static bool CanCancel(
        FlightBookingStatus status,
        DateTime departure,
        DateTime now)
        => CancellationPolicy.ForFlight(status, departure, 0, now).Allowed;

    public static int SeatsLeft(int capacity, int taken)
        => Math.Max(0, capacity - taken);

    // 200 -> "3h 20m"
    public static string FormatDuration(int minutes)
    {
        var h = minutes / 60;
        var m = minutes % 60;

        return h == 0 ? $"{m}m" : m == 0 ? $"{h}h" : $"{h}h {m}m";
    }
}
