namespace HotelTravel.Application.DTOs.Hotels;

public class UpdateHotelDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string Country { get; set; }

    public string Phone { get; set; }
    public string Email { get; set; }

    public string MainImage { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public int StarRating { get; set; }

    public int? HotelChainId { get; set; }
}