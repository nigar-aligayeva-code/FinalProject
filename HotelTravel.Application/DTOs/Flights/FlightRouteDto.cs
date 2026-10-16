namespace HotelTravel.Application.DTOs.Flights;

// Admin: reys cədvəli
public class FlightRouteDto
{
    public int Id { get; set; }

    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;

    public string FromCity { get; set; } = string.Empty;
    public string FromCode { get; set; } = string.Empty;
    public string ToCity { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;

    public TimeSpan DepartureTime { get; set; }
    public int DurationMinutes { get; set; }

    public decimal EconomyPrice { get; set; }
    public decimal BusinessPrice { get; set; }

    public int EconomySeats { get; set; }
    public int BusinessSeats { get; set; }

    public bool IsActive { get; set; }
}
