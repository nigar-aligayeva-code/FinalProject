namespace HotelTravel.Domain.Entities;

public class HotelImage : BaseEntity
{
    public string ImageUrl { get; set; }

    public string Category { get; set; }

    public int HotelId { get; set; }

    public Hotel Hotel { get; set; }
}