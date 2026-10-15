using System.ComponentModel.DataAnnotations;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Taxis;

public class CreateTaxiBookingDto
{
    [Range(1, int.MaxValue)]
    public int HotelId { get; set; }

    public TaxiDirection Direction { get; set; }

    [Required]
    public DateTime PickupTime { get; set; }

    [Range(1, 7)]
    public int Passengers { get; set; } = 2;

    [Range(0, 7)]
    public int Luggage { get; set; } = 2;

    public TaxiVehicleType VehicleType { get; set; }

    [StringLength(10)]
    [RegularExpression(@"^[A-Za-z0-9]{2}\s?\d{1,4}[A-Za-z]?$",
        ErrorMessage = "Enter a flight number like J2 8012.")]
    [Display(Name = "Flight number")]
    public string? FlightNumber { get; set; }

    [Required(ErrorMessage = "Passenger name is required.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string PassengerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Note for the driver")]
    public string? Notes { get; set; }
}
