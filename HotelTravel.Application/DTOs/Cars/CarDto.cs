using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Cars;

public class CarDto
{
    public int Id { get; set; }

    public string City { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public CarCategory Category { get; set; }

    public int Seats { get; set; }
    public bool IsAutomatic { get; set; }
    public bool HasAirConditioning { get; set; }

    public decimal PricePerDay { get; set; }
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    // Axtarışda seçilmiş tarixlər üçün
    public bool IsAvailable { get; set; } = true;
    public int Days { get; set; }
    public decimal TotalPrice { get; set; }
}
