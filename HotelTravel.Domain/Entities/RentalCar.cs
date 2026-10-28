using HotelTravel.Domain.Enums;

namespace HotelTravel.Domain.Entities;

// İcarəyə verilən maşın (şəhər üzrə park)
public class RentalCar : BaseEntity
{
    // Otelin City dəyəri ilə eyni (məs. "Baku")
    public string City { get; set; } = string.Empty;

    // Məs. "Toyota Corolla"
    public string Model { get; set; } = string.Empty;

    public CarCategory Category { get; set; }

    public int Seats { get; set; }

    public bool IsAutomatic { get; set; } = true;

    public bool HasAirConditioning { get; set; } = true;

    public decimal PricePerDay { get; set; }

    public string? ImageUrl { get; set; }

    // Parkda bu modeldən neçə maşın var (məs. 3 Renault Clio).
    // Hamısı eyni günlərə icarədə olanda model "tutulub" görünür.
    public int Quantity { get; set; } = 1;

    // Admin müvəqqəti olaraq parkdan çıxara bilər
    public bool IsActive { get; set; } = true;

    public ICollection<CarRental> Rentals { get; set; } = new List<CarRental>();
}
