using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Services;

/// <summary>Ləğvin nəticəsi: olarmı, neçə faiz cərimə, neçə AZN.</summary>
public record CancellationQuote(bool Allowed, int FeePercent, decimal Fee, string? Error)
{
    public bool IsFree => Allowed && Fee == 0;

    public static CancellationQuote No(string error) => new(false, 0, 0, error);
}

/// <summary>
/// Ləğv qaydaları (otel və uçuş). Qonaq istədiyi an pulsuz ləğv edə bilməz:
///   • 3 gün (72 saat) və daha çox qalıbsa  -> pulsuz
///   • 1–3 gün qalıbsa                       -> 30% cərimə
///   • 24 saatdan az qalıbsa                 -> 50% cərimə
///   • otel: giriş günü, uçuş: son 3 saat    -> ləğv olmur
/// Otel hələ təsdiqləməyən sorğu (Pending) həmişə pulsuz ləğv olunur.
/// Rəqəmləri dəyişmək üçün yalnız bu sinifdəki sabitlər dəyişdirilir.
/// </summary>
public static class CancellationPolicy
{
    // Otelə giriş saatı (məktubda da "from 14:00" yazılır)
    public const int CheckInHour = 14;

    public const int FreeHours = 72;
    public const int LateHours = 24;

    public const int LateFeePercent = 30;
    public const int LastMinuteFeePercent = 50;

    // Uçuşa bu qədər saat qalmış ləğv olmur
    public const int FlightClosedHours = 3;

    /// <summary>Otel rezervasiyası. now - serverin yerli vaxtı (DateTime.Now).</summary>
    public static CancellationQuote ForHotel(
        BookingStatus status,
        DateTime checkInDate,
        decimal total,
        DateTime now)
    {
        if (status == BookingStatus.Cancelled)
            return CancellationQuote.No("This booking is already cancelled.");

        if (status != BookingStatus.Confirmed && status != BookingStatus.Pending)
            return CancellationQuote.No("This booking can no longer be cancelled.");

        if (checkInDate.Date <= now.Date)
            return CancellationQuote.No("Bookings can only be cancelled before the check-in day.");

        // Otel hələ "hə" deməyib - qonaqdan pul tutmaq ədalətli deyil
        if (status == BookingStatus.Pending)
            return new CancellationQuote(true, 0, 0, null);

        var hoursLeft = (checkInDate.Date.AddHours(CheckInHour) - now).TotalHours;

        return Quote(PercentFor(hoursLeft), total);
    }

    /// <summary>Aviabilet.</summary>
    public static CancellationQuote ForFlight(
        FlightBookingStatus status,
        DateTime departure,
        decimal total,
        DateTime now)
    {
        if (status != FlightBookingStatus.Confirmed)
            return CancellationQuote.No("This flight can no longer be cancelled.");

        var hoursLeft = (departure - now).TotalHours;

        if (hoursLeft < FlightClosedHours)
            return CancellationQuote.No($"Flights cannot be cancelled in the last {FlightClosedHours} hours before departure.");

        return Quote(PercentFor(hoursLeft), total);
    }

    /// <summary>Neçə saat qalıb -> cərimə faizi.</summary>
    public static int PercentFor(double hoursLeft) =>
        hoursLeft >= FreeHours ? 0
        : hoursLeft >= LateHours ? LateFeePercent
        : LastMinuteFeePercent;

    public static decimal FeeFor(decimal total, int percent) =>
        Math.Round(total * percent / 100m, 2, MidpointRounding.AwayFromZero);

    // Pulsuz ləğvin son anı (otel): girişdən 72 saat əvvəl
    public static DateTime HotelFreeUntil(DateTime checkInDate) =>
        checkInDate.Date.AddHours(CheckInHour).AddHours(-FreeHours);

    private static CancellationQuote Quote(int percent, decimal total) =>
        new(true, percent, FeeFor(total, percent), null);
}
