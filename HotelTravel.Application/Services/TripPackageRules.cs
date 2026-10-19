using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Services;

// Trip paketinin qaydaları (unit test üçün ayrıca)
public static class TripPackageRules
{
    public const int MaxTravelers = 6;   // uçuşdakı limitlə eyni
    public const int MaxNights = 30;

    // Endirim pillələri
    public const int BaseDiscount = 8;        // uçuş + otel
    public const int ReturnFlightBonus = 2;   // gediş-dönüş
    public const int TaxiBonus = 2;           // hava limanı taksisi

    // Taksi uçuş enəndən bu qədər sonra gözləyir (pasport + baqaj)
    public const int TaxiPickupDelayMinutes = 45;

    public static int GetDiscountPercent(bool hasReturnFlight, bool hasTaxi)
        => BaseDiscount +
           (hasReturnFlight ? ReturnFlightBonus : 0) +
           (hasTaxi ? TaxiBonus : 0);

    public static decimal GetDiscountAmount(decimal subtotal, int percent)
        => subtotal <= 0 || percent <= 0
            ? 0
            : Math.Round(subtotal * percent / 100m, 2, MidpointRounding.AwayFromZero);

    public static int GetNights(DateTime departDate, DateTime returnDate)
        => Math.Max(0, (returnDate.Date - departDate.Date).Days);

    public static DateTime GetTaxiPickup(DateTime flightArrival)
        => flightArrival.AddMinutes(TaxiPickupDelayMinutes);

    // Səyahətçilərə sığan ən ucuz maşın (hər nəfərə 1 baqaj)
    public static TaxiVehicleType? ChooseVehicle(int travelers)
    {
        var luggage = Math.Min(travelers, TaxiRules.MaxLuggage);

        return TaxiRules.Vehicles
            .Where(v => TaxiRules.Fits(v.Type, travelers, luggage))
            .OrderBy(v => v.BaseFare)
            .Select(v => (TaxiVehicleType?)v.Type)
            .FirstOrDefault();
    }

    public static string? ValidateSearch(
        string? fromCity,
        string? toCity,
        DateTime? departDate,
        DateTime? returnDate,
        int travelers,
        DateTime today)
    {
        if (string.IsNullOrWhiteSpace(fromCity) || string.IsNullOrWhiteSpace(toCity))
            return "Please choose where you fly from and where you're going.";

        if (string.Equals(fromCity.Trim(), toCity.Trim(), StringComparison.OrdinalIgnoreCase))
            return "Departure and destination must be different cities.";

        if (!departDate.HasValue || !returnDate.HasValue)
            return "Please choose your travel dates.";

        if (departDate.Value.Date <= today.Date)
            return "Trips can start from tomorrow.";

        var nights = GetNights(departDate.Value, returnDate.Value);

        if (nights < 1)
            return "The return date must be after the departure date.";

        if (nights > MaxNights)
            return $"A trip can be up to {MaxNights} nights.";

        if (travelers < 1 || travelers > MaxTravelers)
            return $"A trip package is for 1 to {MaxTravelers} travelers.";

        return null;
    }
}
