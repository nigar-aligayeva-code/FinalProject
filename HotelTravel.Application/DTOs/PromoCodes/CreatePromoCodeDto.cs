using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.PromoCodes;

public class CreatePromoCodeDto
{
    [Required(ErrorMessage = "Code is required.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Code must be 3-30 characters.")]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Use only letters, numbers, - and _.")]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [Range(1, 90, ErrorMessage = "Discount must be between 1 and 90%.")]
    public int DiscountPercent { get; set; } = 10;

    [DataType(DataType.Date)]
    public DateTime? ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }

    [Range(1, 100000, ErrorMessage = "Max uses must be at least 1.")]
    public int? MaxUses { get; set; }
}
