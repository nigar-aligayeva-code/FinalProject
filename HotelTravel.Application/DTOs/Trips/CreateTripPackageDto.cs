using System.ComponentModel.DataAnnotations;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Trips;

public class CreateTripPackageDto
{
    [Required]
    public string FromCity { get; set; } = string.Empty;

    [Required]
    public string ToCity { get; set; } = string.Empty;

    [Required]
    public DateTime DepartDate { get; set; }

    [Required]
    public DateTime ReturnDate { get; set; }

    [Range(1, 6)]
    public int Adults { get; set; } = 1;

    [Range(0, 5)]
    public int Children { get; set; }

    public CabinClass Cabin { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose an outbound flight.")]
    public int OutboundRouteId { get; set; }

    // null -> dönüş uçuşu paketdə deyil
    public int? ReturnRouteId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a room.")]
    public int RoomId { get; set; }

    public bool IncludeTaxi { get; set; }

    // Hər səyahətçinin pasportdakı adı
    public List<string> TravelerNames { get; set; } = new();

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;
}
