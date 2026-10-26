namespace HotelTravel.Domain.Enums;

// Rezervasiyanın ödəniş vəziyyəti (Stripe test rejimi)
public enum PaymentStatus
{
    Unpaid,     // otelə gələndə ödəyəcək və ya hələ onlayn ödəməyib
    Paid,       // Stripe ilə onlayn ödənilib
    Refunded    // ləğv edilib, pul (cərimə çıxılmaqla) karta qaytarılıb
}
