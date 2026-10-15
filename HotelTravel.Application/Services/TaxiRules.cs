using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Services;

// Maşın sinfinin göstəriciləri
public record TaxiVehicleInfo(
    TaxiVehicleType Type,
    string Name,
    string Example,
    int MaxPassengers,
    int MaxLuggage,
    decimal BaseFare,
    decimal PricePerKm);

// Otel şəhərinin hava limanı
public record AirportInfo(
    string City,
    string Code,
    string Name,
    int DistanceKm);

// Hava limanı taksisi qaydaları (unit test üçün ayrıca)
public static class TaxiRules
{
    // Sifariş götürülmədən ən azı 3 saat əvvəl verilməlidir
    public const int MinHoursAhead = 3;

    // Ən çox 180 gün qabağa
    public const int MaxDaysAhead = 180;

    // Pulsuz ləğv: götürülmədən 24 saat əvvələ qədər
    public const int FreeCancelHours = 24;

    public const int MaxPassengers = 7;
    public const int MaxLuggage = 7;

    public static readonly IReadOnlyList<TaxiVehicleInfo> Vehicles = new[]
    {
        new TaxiVehicleInfo(TaxiVehicleType.Standard, "Standard", "Toyota Camry or similar", 3, 2, 15m, 1.0m),
        new TaxiVehicleInfo(TaxiVehicleType.Comfort, "Comfort", "Mercedes-Benz E-Class or similar", 3, 3, 25m, 1.5m),
        new TaxiVehicleInfo(TaxiVehicleType.Minivan, "Minivan", "Mercedes-Benz Vito or similar", 7, 7, 35m, 1.8m),
        new TaxiVehicleInfo(TaxiVehicleType.Executive, "Executive", "Mercedes-Benz S-Class or similar", 3, 3, 60m, 2.5m)
    };

    // Məsafə: hava limanından şəhər mərkəzinə (təxmini)
    public static readonly IReadOnlyList<AirportInfo> Airports = new[]
    {
        new AirportInfo("Baku", "GYD", "Heydar Aliyev International Airport", 25),
        new AirportInfo("Gabala", "GBB", "Gabala International Airport", 20),
        new AirportInfo("Tbilisi", "TBS", "Tbilisi International Airport", 18),
        new AirportInfo("Batumi", "BUS", "Batumi International Airport", 5),
        new AirportInfo("Istanbul", "IST", "Istanbul Airport", 40),
        new AirportInfo("Antalya", "AYT", "Antalya Airport", 13),
        new AirportInfo("Dubai", "DXB", "Dubai International Airport", 15)
    };

    public static AirportInfo? GetAirport(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return null;

        return Airports.FirstOrDefault(a =>
            string.Equals(a.City, city.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static TaxiVehicleInfo GetVehicle(TaxiVehicleType type)
        => Vehicles.First(v => v.Type == type);

    // Qiymət = başlanğıc + km × tarif, tam manata yuvarlaqlaşdırılır
    public static decimal CalculatePrice(TaxiVehicleType type, int distanceKm)
    {
        var vehicle = GetVehicle(type);
        var km = Math.Max(0, distanceKm);

        return Math.Round(
            vehicle.BaseFare + km * vehicle.PricePerKm,
            0,
            MidpointRounding.AwayFromZero);
    }

    // Təxmini yol vaxtı (dəqiqə)
    public static int EstimateMinutes(int distanceKm)
        => 10 + (int)Math.Round(Math.Max(0, distanceKm) * 1.4);

    // Sərnişinlər və baqaj bu maşına sığır?
    public static bool Fits(TaxiVehicleType type, int passengers, int luggage)
    {
        var vehicle = GetVehicle(type);

        return passengers >= 1 &&
               luggage >= 0 &&
               passengers <= vehicle.MaxPassengers &&
               luggage <= vehicle.MaxLuggage;
    }

    public static string? ValidateRequest(
        DateTime pickupTime,
        int passengers,
        int luggage,
        DateTime now)
    {
        if (passengers < 1 || passengers > MaxPassengers)
            return $"Passengers must be between 1 and {MaxPassengers}.";

        if (luggage < 0 || luggage > MaxLuggage)
            return $"Luggage must be between 0 and {MaxLuggage}.";

        if (pickupTime < now.AddHours(MinHoursAhead))
            return $"Please book at least {MinHoursAhead} hours before pick-up.";

        if (pickupTime.Date > now.Date.AddDays(MaxDaysAhead))
            return $"Taxis can be booked up to {MaxDaysAhead} days ahead.";

        return null;
    }

    // Qonaq götürülmədən 24 saat əvvələ qədər ləğv edə bilər
    public static bool CanCancel(
        TaxiBookingStatus status,
        DateTime pickupTime,
        DateTime now)
        => status == TaxiBookingStatus.Confirmed &&
           pickupTime >= now.AddHours(FreeCancelHours);
}
