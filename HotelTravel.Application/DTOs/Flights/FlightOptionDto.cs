using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Flights;

// Axtarış nəticəsində konkret uçuş (marşrut + tarix)
public class FlightOptionDto
{
    public int RouteId { get; set; }

    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;

    public string FromCity { get; set; } = string.Empty;
    public string FromCode { get; set; } = string.Empty;
    public string FromAirport { get; set; } = string.Empty;

    public string ToCity { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;
    public string ToAirport { get; set; } = string.Empty;

    public DateTime Departure { get; set; }
    public DateTime Arrival { get; set; }
    public int DurationMinutes { get; set; }

    public CabinClass Cabin { get; set; }

    // Bir sərnişin üçün və cəmi
    public decimal Fare { get; set; }
    public decimal Total { get; set; }

    public int SeatsLeft { get; set; }

    // Kifayət qədər yer var və uçuşa vaxt qalıb
    public bool IsAvailable { get; set; }

    public bool ArrivesNextDay => Arrival.Date > Departure.Date;
}
