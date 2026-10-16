using HotelTravel.Domain.Enums;

namespace HotelTravel.Domain.Entities;

// Aviabilet rezervasiyası
public class FlightBooking : BaseEntity
{
    public string ConfirmationCode { get; set; } = string.Empty;

    public int FlightRouteId { get; set; }
    public FlightRoute FlightRoute { get; set; } = null!;

    // Uçuş tarixi (yerin hesablanması üçün)
    public DateTime FlightDate { get; set; }

    // Sifariş anındakı məlumat saxlanılır
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string FromCity { get; set; } = string.Empty;
    public string FromCode { get; set; } = string.Empty;
    public string ToCity { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;

    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }

    public CabinClass Cabin { get; set; }

    public int Passengers { get; set; }

    // Sərnişinlərin adları (hər sətirdə bir ad)
    public string PassengerNames { get; set; } = string.Empty;

    public decimal FarePerPassenger { get; set; }
    public decimal TotalPrice { get; set; }

    public FlightBookingStatus Status { get; set; }

    // Əlaqə
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    // Daxil olmuş istifadəçi (My Bookings üçün)
    public string? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
}
