using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Trips;

// Axtarış nəticəsi: seçmək üçün uçuşlar, otaqlar və taksi təklifi
public class TripPlanDto
{
    public string? Error { get; set; }

    public int Nights { get; set; }

    public List<FlightOptionDto> OutboundFlights { get; set; } = new();
    public List<FlightOptionDto> ReturnFlights { get; set; } = new();

    // Otaq + bu gecələr üçün təxmini qiymət
    public List<TripRoomOptionDto> Rooms { get; set; } = new();

    public TripTaxiOptionDto? Taxi { get; set; }

    // Endirim pillələri (səhifədə göstərmək üçün)
    public int BaseDiscount { get; set; }
    public int ReturnFlightBonus { get; set; }
    public int TaxiBonus { get; set; }
}

public class TripRoomOptionDto
{
    public GetRoomDto Room { get; set; } = new();
    public decimal StayPrice { get; set; }
}

public class TripTaxiOptionDto
{
    public TaxiVehicleType Vehicle { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string AirportName { get; set; } = string.Empty;
    public int DistanceKm { get; set; }
    public decimal Price { get; set; }
}
