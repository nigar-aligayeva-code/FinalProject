using HotelTravel.Domain.Enums;

namespace HotelTravel.Domain.Entities;

// Hava limanı taksisi (transfer) sifarişi
public class TaxiBooking : BaseEntity
{
    public string ConfirmationCode { get; set; } = string.Empty;

    // Otel (transferin bir ucu)
    public int HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;

    // Hava limanı (sifariş anındakı məlumat saxlanılır)
    public string City { get; set; } = string.Empty;
    public string AirportCode { get; set; } = string.Empty;
    public string AirportName { get; set; } = string.Empty;

    public TaxiDirection Direction { get; set; }

    // Götürülmə vaxtı (yerli vaxt)
    public DateTime PickupTime { get; set; }

    public string? FlightNumber { get; set; }

    public int Passengers { get; set; }
    public int Luggage { get; set; }

    public TaxiVehicleType VehicleType { get; set; }

    public int DistanceKm { get; set; }
    public decimal Price { get; set; }

    public TaxiBookingStatus Status { get; set; }

    // Sərnişin
    public string PassengerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Notes { get; set; }

    // Daxil olmuş istifadəçi (My Bookings üçün)
    public string? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
}
