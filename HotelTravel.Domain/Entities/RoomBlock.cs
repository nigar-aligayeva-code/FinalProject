namespace HotelTravel.Domain.Entities;

// Bağlı tarixlər: otel otağı başqa yerdə satıb, təmirdədir və s.
// Bu günlərdə otaq axtarışda görünmür və rezervasiya edilə bilmir.
// Tarixlər rezervasiya kimidir: StartDate daxil, EndDate daxil deyil (çıxış günü).
public class RoomBlock : BaseEntity
{
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Məs. "Sold on Booking.com", "Renovation"
    public string? Reason { get; set; }

    // Kim bağlayıb (partnyor və ya admin)
    public string? CreatedByUserId { get; set; }
}
