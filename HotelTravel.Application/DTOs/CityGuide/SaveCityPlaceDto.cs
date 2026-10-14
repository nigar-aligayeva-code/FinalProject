using HotelTravel.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.CityGuide;

// Admin: yeni yer yaratmaq və ya redaktə etmək
public class SaveCityPlaceDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public PlaceCategory Category { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(600, MinimumLength = 10, ErrorMessage = "Description must be 10-600 characters.")]
    public string Description { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(400)]
    public string? ImageUrl { get; set; }

    [Range(0, 100000, ErrorMessage = "Price must be positive.")]
    public decimal? PriceFrom { get; set; }

    [StringLength(50)]
    public string? Duration { get; set; }

    [Range(0, 1000)]
    public int SortOrder { get; set; }
}
