namespace HotelTravel.Application.DTOs.Bookings;

// Rezervasiya formundakı qiymət xülasəsi və yekun hesablama
public class PriceQuoteDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }

    public int Nights { get; set; }
    public decimal PricePerNight { get; set; }

    public decimal BaseTotal { get; set; }
    public int DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    // "Promo code SUMMER10" / "Explorer reward..."
    public string? DiscountNote { get; set; }

    // Tətbiq olunan promo kod (UsedCount artırmaq üçün)
    public string? AppliedPromoCode { get; set; }

    // Daxil edilmiş kod yanlışdırsa
    public string? PromoError { get; set; }

    // Explorer reward
    public bool LoyaltyEligible { get; set; }
    public string? LoyaltyCity { get; set; }
}
