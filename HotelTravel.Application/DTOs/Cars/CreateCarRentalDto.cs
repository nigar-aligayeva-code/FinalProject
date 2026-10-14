using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Cars;

public class CreateCarRentalDto
{
    [Range(1, int.MaxValue)]
    public int CarId { get; set; }

    [Required]
    public DateTime PickupDate { get; set; }

    [Required]
    public DateTime ReturnDate { get; set; }

    [Required(ErrorMessage = "Driver name is required.")]
    [StringLength(100)]
    public string DriverName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    // Sürücünün yaşı və vəsiqəsi təsdiqi
    [Range(typeof(bool), "true", "true", ErrorMessage = "Please confirm you have a valid driving licence.")]
    public bool HasLicence { get; set; }
}
