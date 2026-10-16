namespace HotelTravel.Application.DTOs.Flights;

// Tarix zolağı: hər gün üçün ən ucuz bilet
public class FlightDayFareDto
{
    public DateTime Date { get; set; }
    public decimal? LowestFare { get; set; }
}
