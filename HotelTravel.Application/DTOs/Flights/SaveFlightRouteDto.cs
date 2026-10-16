using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Flights;

// Admin: reys əlavə et / redaktə et
public class SaveFlightRouteDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Flight number is required.")]
    [StringLength(10)]
    [RegularExpression(@"^[A-Za-z0-9]{2}\s?\d{1,4}$", ErrorMessage = "Use a format like J2 8011.")]
    [Display(Name = "Flight number")]
    public string FlightNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Airline is required.")]
    [StringLength(60)]
    public string Airline { get; set; } = string.Empty;

    [Required(ErrorMessage = "Departure city is required.")]
    [StringLength(100)]
    [Display(Name = "From")]
    public string FromCity { get; set; } = string.Empty;

    [Required(ErrorMessage = "Destination city is required.")]
    [StringLength(100)]
    [Display(Name = "To")]
    public string ToCity { get; set; } = string.Empty;

    [Display(Name = "Departure time")]
    public TimeSpan DepartureTime { get; set; } = new(9, 0, 0);

    [Range(20, 900, ErrorMessage = "Duration must be between 20 and 900 minutes.")]
    [Display(Name = "Duration (minutes)")]
    public int DurationMinutes { get; set; } = 120;

    [Range(1, 20000, ErrorMessage = "Economy price must be positive.")]
    [Display(Name = "Economy price (AZN)")]
    public decimal EconomyPrice { get; set; } = 150;

    [Range(0, 50000)]
    [Display(Name = "Business price (AZN)")]
    public decimal BusinessPrice { get; set; } = 450;

    [Range(1, 500)]
    [Display(Name = "Economy seats")]
    public int EconomySeats { get; set; } = 150;

    [Range(0, 100)]
    [Display(Name = "Business seats")]
    public int BusinessSeats { get; set; } = 12;

    public bool IsActive { get; set; } = true;
}
