using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Profile;

// Profil məlumatlarının yenilənməsi (email dəyişmir)
public class UpdateProfileDto
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }
}
