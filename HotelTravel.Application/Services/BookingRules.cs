using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Services;

// Rezervasiya qaydaları bir yerdə saxlanılır ki,
// həm servislər istifadə etsin, həm də unit test yazmaq asan olsun.
public static class BookingRules
{
    // Qonaq rezervasiyanı dəyişə / ləğv edə bilərmi?
    // Yalnız Confirmed və giriş günü hələ gəlməyibsə.
    public static string? CanGuestChange(
        BookingStatus status,
        DateTime checkInDate,
        DateTime today)
    {
        if (status == BookingStatus.Cancelled)
            return "This booking is already cancelled.";

        if (status != BookingStatus.Confirmed)
            return "This booking can no longer be changed.";

        if (checkInDate.Date <= today.Date)
            return "Bookings can only be changed before the check-in day.";

        return null;
    }


    // Yeni tarix və qonaq sayı düzgündürmü?
    public static string? ValidateStay(
        DateTime checkInDate,
        DateTime checkOutDate,
        int adultCount,
        int childrenCount,
        int roomCapacity,
        DateTime today)
    {
        if (checkInDate.Date < today.Date)
            return "Check-in date cannot be in the past.";

        if (checkInDate.Date >= checkOutDate.Date)
            return "Check-out date must be after check-in date.";

        if (adultCount <= 0)
            return "At least one adult is required.";

        if (childrenCount < 0)
            return "Children count cannot be negative.";

        if (adultCount + childrenCount > roomCapacity)
            return $"This room allows up to {roomCapacity} guests.";

        return null;
    }


    // F6 - Ümumi qiymət: gecə sayı × gecəlik qiymət
    public static decimal CalculateTotal(
        decimal pricePerNight,
        DateTime checkInDate,
        DateTime checkOutDate)
    {
        var nights = (checkOutDate.Date - checkInDate.Date).Days;

        return nights <= 0
            ? 0
            : nights * pricePerNight;
    }
}
