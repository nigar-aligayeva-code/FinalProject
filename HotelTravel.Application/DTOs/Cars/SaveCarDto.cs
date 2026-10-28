using HotelTravel.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Cars;

// Admin: maşın əlavə et / redaktə et
public class SaveCarDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required.")]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;

    public CarCategory Category { get; set; }

    [Range(2, 9, ErrorMessage = "Seats must be between 2 and 9.")]
    public int Seats { get; set; } = 5;

    public bool IsAutomatic { get; set; } = true;

    public bool HasAirConditioning { get; set; } = true;

    [Range(1, 10000, ErrorMessage = "Price per day must be positive.")]
    public decimal PricePerDay { get; set; } = 60;

    [StringLength(400)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    // Bu modeldən parkda neçə maşın var
    [Range(1, 50, ErrorMessage = "Number of cars must be between 1 and 50.")]
    public int Quantity { get; set; } = 3;
}
