using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.CityGuide;

public class CityPlaceDto
{
    public int Id { get; set; }

    public string City { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PlaceCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? ImageUrl { get; set; }

    public decimal? PriceFrom { get; set; }
    public string? Duration { get; set; }

    public int SortOrder { get; set; }
}
