using HotelTravel.Domain.Enums;

namespace HotelTravel.Domain.Entities;

// Şəhər bələdçisində bir yer (görməli yer, tur, restoran, alış-veriş)
public class CityPlace : BaseEntity
{
    // Otelin City dəyəri ilə eyni olmalıdır (məs. "Baku")
    public string City { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public PlaceCategory Category { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? ImageUrl { get; set; }

    // Xəritədə yeri (kliklənəndə açılan pəncərədə göstərilir)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Turlar üçün
    public decimal? PriceFrom { get; set; }
    public string? Duration { get; set; }

    // Siyahıda sıra
    public int SortOrder { get; set; }
}
