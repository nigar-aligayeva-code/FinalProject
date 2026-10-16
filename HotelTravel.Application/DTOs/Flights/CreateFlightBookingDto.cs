using System.ComponentModel.DataAnnotations;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Flights;

public class CreateFlightBookingDto
{
    [Range(1, int.MaxValue)]
    public int RouteId { get; set; }

    [Required]
    public DateTime FlightDate { get; set; }

    public CabinClass Cabin { get; set; }

    [Range(1, 6)]
    public int Passengers { get; set; } = 1;

    // Hər sərnişin üçün pasportdakı ad
    public List<string> PassengerNames { get; set; } = new();

    [Required(ErrorMessage = "Contact name is required.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string ContactName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;
}
