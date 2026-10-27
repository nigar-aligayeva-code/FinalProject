using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Availability;

// Bağlı tarix (otaq başqa yerdə satılıb, təmir və s.)
public class RoomBlockDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Yeni bağlı tarix: RoomId boşdursa otelin bütün otaqları bağlanır
public class CreateRoomBlockDto
{
    public int? RoomId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);

    [StringLength(200)]
    public string? Reason { get; set; }
}

public class UpdateRoomPriceDto
{
    [Range(1, 100000, ErrorMessage = "Price must be between 1 and 100000.")]
    public decimal PricePerNight { get; set; }
}

public class AvailabilityRoomDto
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal PricePerNight { get; set; }
    public string? MainImage { get; set; }

    // Gələcək rezervasiyalar (gözləyən + təsdiqlənmiş)
    public int UpcomingBookings { get; set; }

    // Bu gün bağlıdır?
    public bool ClosedToday { get; set; }
}

// Otelin boş yer paneli (partnyor və admin)
public class HotelAvailabilityDto
{
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public DateTime? AvailabilityUpdatedAt { get; set; }
    public bool IsFresh { get; set; }

    public int PendingRequests { get; set; }
    public int PartnerCount { get; set; }

    public List<AvailabilityRoomDto> Rooms { get; set; } = new();
    public List<RoomBlockDto> Blocks { get; set; } = new();
}

// Gözləyən sorğu üçün yoxlama: təsdiqləmək olarmı?
public class AvailabilityCheckDto
{
    public int BookingId { get; set; }
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;

    // Bizdə həmin otağa üst-üstə düşən başqa (aktiv) rezervasiya
    public int OverlappingBookings { get; set; }

    // Otel həmin günləri bağlayıb?
    public bool IsClosedByHotel { get; set; }
    public List<string> ClosedReasons { get; set; } = new();

    public DateTime? AvailabilityUpdatedAt { get; set; }
    public bool IsFresh { get; set; }

    // Otelin öz partnyor hesabı var? (yoxdursa admin otelə zəng edir)
    public bool HasPartner { get; set; }

    public bool LooksFree => OverlappingBookings == 0 && !IsClosedByHotel;
}

// Partnyor: hansı otelə bağlıyam?
public class PartnerMeDto
{
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

// Admin: partnyor hesabları
public class PartnerAccountDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
}

public class CreatePartnerDto
{
    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Choose a hotel.")]
    public int HotelId { get; set; }
}
