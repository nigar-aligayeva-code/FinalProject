using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.DTOs.Taxis;

namespace HotelTravel.Application.DTOs.Trips;

public class TripPackageDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public string FromCity { get; set; } = string.Empty;
    public string ToCity { get; set; } = string.Empty;
    public DateTime DepartDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public int Nights { get; set; }
    public int Travelers { get; set; }

    public decimal HotelPrice { get; set; }
    public decimal FlightsPrice { get; set; }
    public decimal TaxiPrice { get; set; }
    public decimal Subtotal { get; set; }
    public int DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    // Hissələr (yalnız tək paket açılanda doldurulur)
    public GetBookingDto? Hotel { get; set; }
    public FlightBookingDto? OutboundFlight { get; set; }
    public FlightBookingDto? ReturnFlight { get; set; }
    public TaxiBookingDto? Taxi { get; set; }

    public string HotelBookingCode { get; set; } = string.Empty;
    public string OutboundFlightCode { get; set; } = string.Empty;
    public string? ReturnFlightCode { get; set; }
    public string? TaxiCode { get; set; }

    // Siyahı üçün
    public string? HotelName { get; set; }
}
