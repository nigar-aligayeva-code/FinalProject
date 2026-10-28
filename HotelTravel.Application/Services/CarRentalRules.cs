using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Services;

// Maşın icarəsi qaydaları (unit test üçün ayrıca)
public static class CarRentalRules
{
    public const int MaxRentalDays = 30;

    public static string? ValidateDates(
        DateTime pickupDate,
        DateTime returnDate,
        DateTime today)
    {
        if (pickupDate.Date < today.Date)
            return "Pick-up date cannot be in the past.";

        if (returnDate.Date <= pickupDate.Date)
            return "Return date must be after pick-up date.";

        if ((returnDate.Date - pickupDate.Date).Days > MaxRentalDays)
            return $"Cars can be rented for up to {MaxRentalDays} days.";

        return null;
    }

    public static int CountDays(DateTime pickupDate, DateTime returnDate)
    {
        var days = (returnDate.Date - pickupDate.Date).Days;
        return days < 1 ? 1 : days;
    }

    public static decimal CalculateTotal(decimal pricePerDay, int days)
        => pricePerDay * days;

    // Tarixlər üst-üstə düşür? (qaytarma günü yeni götürmə ola bilər)
    public static bool Overlaps(
        DateTime pickupA, DateTime returnA,
        DateTime pickupB, DateTime returnB)
        => pickupA.Date < returnB.Date && returnA.Date > pickupB.Date;

    /// <summary>
    /// Seçilən günlərdə eyni anda neçə maşın icarədədir (ən çox olan gün).
    /// Məs. 1-3 və 3-5 icarələri eyni maşınla ola bilər (biri qaytarılan gün digəri götürür) - 1 sayılır.
    /// </summary>
    public static int PeakInUse(
        IEnumerable<(DateTime Pickup, DateTime Return)> rentals,
        DateTime pickup,
        DateTime dropoff)
    {
        var list = rentals.ToList();
        var peak = 0;

        for (var day = pickup.Date; day < dropoff.Date; day = day.AddDays(1))
        {
            var inUse = list.Count(r => r.Pickup.Date <= day && day < r.Return.Date);
            peak = Math.Max(peak, inUse);
        }

        return peak;
    }

    /// <summary>Seçilən tarixlərdə boş qalan maşın sayı.</summary>
    public static int FreeCount(int quantity, int peakInUse) => Math.Max(0, quantity - peakInUse);

    // Qonaq götürmə günündən əvvəl ləğv edə bilər
    public static bool CanCancel(
        CarRentalStatus status,
        DateTime pickupDate,
        DateTime today)
        => status == CarRentalStatus.Confirmed && pickupDate.Date > today.Date;
}
