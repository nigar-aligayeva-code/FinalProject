namespace HotelTravel.Domain.Entities;

// Trip paketi: uçuş(lar) + otel + (istəyə bağlı) taksi bir sifarişdə.
// Hər hissə öz cədvəlində saxlanılır, burada yalnız onların kodları və paket endirimi var.
public class TripPackage : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string AppUserId { get; set; } = string.Empty;
    public AppUser? AppUser { get; set; }

    public string FromCity { get; set; } = string.Empty;
    public string ToCity { get; set; } = string.Empty;

    public DateTime DepartDate { get; set; }
    public DateTime ReturnDate { get; set; }

    public int Travelers { get; set; }

    // Hissələrin təsdiq kodları
    public string HotelBookingCode { get; set; } = string.Empty;
    public string OutboundFlightCode { get; set; } = string.Empty;
    public string? ReturnFlightCode { get; set; }
    public string? TaxiCode { get; set; }

    // Qiymət
    public decimal HotelPrice { get; set; }
    public decimal FlightsPrice { get; set; }
    public decimal TaxiPrice { get; set; }
    public decimal Subtotal { get; set; }
    public int DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
}
