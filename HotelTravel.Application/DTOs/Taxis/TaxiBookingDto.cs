using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Taxis;

public class TaxiBookingDto
{
    public int Id { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;

    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelAddress { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;
    public string AirportCode { get; set; } = string.Empty;
    public string AirportName { get; set; } = string.Empty;

    public TaxiDirection Direction { get; set; }
    public DateTime PickupTime { get; set; }
    public string? FlightNumber { get; set; }

    public int Passengers { get; set; }
    public int Luggage { get; set; }

    public TaxiVehicleType VehicleType { get; set; }
    public int DistanceKm { get; set; }
    public decimal Price { get; set; }

    public TaxiBookingStatus Status { get; set; }

    public string PassengerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public bool CanCancel { get; set; }

    // "Hava limanı → Otel" və ya "Otel → Hava limanı"
    public string FromText => Direction == TaxiDirection.FromAirport
        ? $"{AirportName} ({AirportCode})"
        : HotelName;

    public string ToText => Direction == TaxiDirection.FromAirport
        ? HotelName
        : $"{AirportName} ({AirportCode})";
}
