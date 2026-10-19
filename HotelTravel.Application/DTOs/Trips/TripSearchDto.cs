using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Trips;

public class TripSearchDto
{
    public string? FromCity { get; set; }
    public string? ToCity { get; set; }

    public DateTime? DepartDate { get; set; }
    public DateTime? ReturnDate { get; set; }

    public int Adults { get; set; } = 2;
    public int Children { get; set; }

    public CabinClass Cabin { get; set; } = CabinClass.Economy;

    public int Travelers => Adults + Children;
}
