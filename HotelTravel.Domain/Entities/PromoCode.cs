namespace HotelTravel.Domain.Entities;

// Admin tərəfindən yaradılan endirim kodu (məs. WELCOME10 = 10%)
public class PromoCode : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    // 1 - 90 %
    public int DiscountPercent { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    // null = limitsiz
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }

    public bool IsActive { get; set; } = true;
}
