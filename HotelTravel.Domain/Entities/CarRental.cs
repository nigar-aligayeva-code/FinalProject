using HotelTravel.Domain.Enums;

namespace HotelTravel.Domain.Entities;

// Maşın icarəsi rezervasiyası
public class CarRental : BaseEntity
{
    public string ConfirmationCode { get; set; } = string.Empty;

    public int RentalCarId { get; set; }
    public RentalCar RentalCar { get; set; } = null!;

    public DateTime PickupDate { get; set; }
    public DateTime ReturnDate { get; set; }

    public int Days { get; set; }
    public decimal PricePerDay { get; set; }
    public decimal TotalPrice { get; set; }

    public CarRentalStatus Status { get; set; }

    // Sürücü
    public string DriverName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    // Daxil olmuş istifadəçi (My Bookings üçün)
    public string? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
}
