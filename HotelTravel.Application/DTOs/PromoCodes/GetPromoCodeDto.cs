namespace HotelTravel.Application.DTOs.PromoCodes;

public class GetPromoCodeDto
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int DiscountPercent { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }

    public bool IsActive { get; set; }

    // Bu gün istifadə oluna bilərmi (aktiv, tarix, limit)
    public bool IsUsableToday { get; set; }
    public string? StatusText { get; set; }
}
