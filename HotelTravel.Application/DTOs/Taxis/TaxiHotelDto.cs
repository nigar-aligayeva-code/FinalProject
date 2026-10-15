namespace HotelTravel.Application.DTOs.Taxis;

// Transfer edilə bilən otel + onun hava limanı
public class TaxiHotelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public string AirportCode { get; set; } = string.Empty;
    public string AirportName { get; set; } = string.Empty;
    public int DistanceKm { get; set; }
    public int EstimatedMinutes { get; set; }
}
