using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Services;

// Rezervasiya qaydaları bir yerdə saxlanılır ki,
// həm servislər istifadə etsin, həm də unit test yazmaq asan olsun.
public static class BookingRules
{
    // Qonaq rezervasiyanı dəyişə / ləğv edə bilərmi?
    // Yalnız Confirmed / Pending və giriş günü hələ gəlməyibsə.
    public static string? CanGuestChange(
        BookingStatus status,
        DateTime checkInDate,
        DateTime today)
    {
        if (status == BookingStatus.Cancelled)
            return "This booking is already cancelled.";

        // Gözləmədə olan sorğunu da qonaq dəyişə / ləğv edə bilər
        if (status != BookingStatus.Confirmed &&
            status != BookingStatus.Pending)
            return "This booking can no longer be changed.";

        if (checkInDate.Date <= today.Date)
            return "Bookings can only be changed before the check-in day.";

        return null;
    }


    // =========================
    // OTEL TƏSDİQİ (sorğu -> təsdiq / rədd)
    // =========================

    // Biz otelin real boş yerlərini bilmirik (otel başqa yerlərdə də satır).
    // Ona görə rezervasiya əvvəl "sorğu" olur, otel (və ya admin) təsdiqləyir.
    public const int HotelAnswerHours = 24;

    // Admin / otel statusu bu keçidlə dəyişə bilərmi?
    public static bool IsValidTransition(BookingStatus from, BookingStatus to) => (from, to) switch
    {
        (BookingStatus.Pending, BookingStatus.Confirmed) => true,
        (BookingStatus.Pending, BookingStatus.Cancelled) => true,
        (BookingStatus.Confirmed, BookingStatus.CheckedIn) => true,
        (BookingStatus.Confirmed, BookingStatus.Cancelled) => true,
        (BookingStatus.CheckedIn, BookingStatus.CheckedOut) => true,
        _ => false
    };

    // Qonaq tarixləri dəyişdi: otel yeni tarixlərdə boş yeri yenidən yoxlamalıdır.
    // Ödənilmiş rezervasiya isə təsdiqli qalır (pulu qaytarma axını yoxdur).
    public static BookingStatus StatusAfterModify(
        BookingStatus current,
        PaymentStatus payment,
        bool datesChanged)
        => current == BookingStatus.Confirmed &&
           payment == PaymentStatus.Unpaid &&
           datesChanged
            ? BookingStatus.Pending
            : current;

    // Sorğu cavabsız nə vaxtdan gözləyir? (admin siyahısında "gecikir" nişanı üçün)
    public static bool IsAnswerOverdue(BookingStatus status, DateTime createdAtUtc, DateTime nowUtc)
        => status == BookingStatus.Pending &&
           nowUtc - createdAtUtc > TimeSpan.FromHours(HotelAnswerHours);


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


    // =========================
    // DISCOUNTS
    // =========================

    // Başqa şəhərdəki otelimizdə qalmış qonağa endirim
    public const int ExplorerRewardPercent = 10;

    // Endirimi tətbiq et (2 rəqəmə yuvarlaqlaşdırılır)
    public static (decimal DiscountAmount, decimal Total) ApplyDiscount(
        decimal baseTotal,
        int discountPercent)
    {
        if (baseTotal <= 0 || discountPercent <= 0)
            return (0, baseTotal < 0 ? 0 : baseTotal);

        var percent = Math.Min(discountPercent, 90);

        var discount = Math.Round(
            baseTotal * percent / 100m,
            2,
            MidpointRounding.AwayFromZero);

        return (discount, baseTotal - discount);
    }


    // Promo kod hazırda istifadə oluna bilərmi?
    public static string? ValidatePromo(
        bool isActive,
        bool isDeleted,
        DateTime? validFrom,
        DateTime? validUntil,
        int? maxUses,
        int usedCount,
        DateTime today)
    {
        if (!isActive || isDeleted)
            return "This promo code is not active.";

        if (validFrom.HasValue && today.Date < validFrom.Value.Date)
            return "This promo code is not valid yet.";

        if (validUntil.HasValue && today.Date > validUntil.Value.Date)
            return "This promo code has expired.";

        if (maxUses.HasValue && usedCount >= maxUses.Value)
            return "This promo code has reached its usage limit.";

        return null;
    }
}
