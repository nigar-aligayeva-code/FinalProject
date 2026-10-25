using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.Services;

/// <summary>Ödənişin təmiz qaydaları (test olunur).</summary>
public static class PaymentRules
{
    /// <summary>Stripe məbləği qəpiklə istəyir: 123.45 AZN -> 12345.</summary>
    public static long ToMinorUnits(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    /// <summary>Ödəmək olarmı? null -> olar, əks halda səbəb.</summary>
    public static string? WhyCannotPay(BookingStatus status, PaymentStatus payment, decimal total)
    {
        if (payment == PaymentStatus.Paid)
            return "This booking is already paid.";

        if (status == BookingStatus.Cancelled)
            return "A cancelled booking cannot be paid.";

        if (status == BookingStatus.CheckedOut)
            return "This stay is already completed.";

        // Stripe-ın minimum məbləği (təxminən 0.50 USD)
        if (total < 1m)
            return "The amount is too small for an online payment.";

        return null;
    }
}
