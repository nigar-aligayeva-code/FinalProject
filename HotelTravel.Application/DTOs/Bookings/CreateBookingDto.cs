
using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Bookings;

public class CreateBookingDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid room.")]
    public int RoomId { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    [Range(1, 20, ErrorMessage = "At least one adult is required.")]
    public int AdultCount { get; set; }

    [Range(0, 20, ErrorMessage = "Children count cannot be negative.")]
    public int ChildrenCount { get; set; }

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    // Könüllü endirim kodu
    [StringLength(30)]
    public string? PromoCode { get; set; }
}
