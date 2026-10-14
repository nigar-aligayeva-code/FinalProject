using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Cars;

public class CarRentalDto
{
    public int Id { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;

    public int CarId { get; set; }
    public string CarModel { get; set; } = string.Empty;
    public CarCategory CarCategory { get; set; }
    public string City { get; set; } = string.Empty;
    public string? CarImage { get; set; }

    public DateTime PickupDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public int Days { get; set; }

    public decimal PricePerDay { get; set; }
    public decimal TotalPrice { get; set; }

    public CarRentalStatus Status { get; set; }

    public string DriverName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public bool CanCancel { get; set; }
}
