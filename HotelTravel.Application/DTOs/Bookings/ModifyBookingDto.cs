using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Bookings;

public class ModifyBookingDto
{
    [Required]
    public string ConfirmationCode { get; set; } = string.Empty;

    // Daxil olmuş sahib üçün lazım deyil,
    // digər hallarda sahibliyi təsdiqləmək üçün tələb olunur
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; }

    [Range(1, 20, ErrorMessage = "At least one adult is required.")]
    public int AdultCount { get; set; }

    [Range(0, 20, ErrorMessage = "Children count cannot be negative.")]
    public int ChildrenCount { get; set; }
}
