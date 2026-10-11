
using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Bookings;

public class CancelBookingDto
{
    [Required]
    public string ConfirmationCode { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
