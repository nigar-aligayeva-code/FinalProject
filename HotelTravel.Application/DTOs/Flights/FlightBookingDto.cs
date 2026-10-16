using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Flights;

public class FlightBookingDto
{
    public int Id { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;

    public int RouteId { get; set; }
    public DateTime FlightDate { get; set; }

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

    // Hər sətirdə bir ad
    public string PassengerNames { get; set; } = string.Empty;

    public decimal FarePerPassenger { get; set; }
    public decimal TotalPrice { get; set; }

    public FlightBookingStatus Status { get; set; }

    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public bool CanCancel { get; set; }

    public List<string> PassengerList =>
        PassengerNames
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}
