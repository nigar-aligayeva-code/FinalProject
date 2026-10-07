using HotelTravel.Domain.Enums;

namespace HotelTravel.Domain.Entities;

public class Room : BaseEntity
{
    public string RoomNumber { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public decimal PricePerNight { get; set; }

    public int Capacity { get; set; }

    public string MainImage { get; set; }
    public string? PanoramaImage { get; set; }
    public RoomType RoomType { get; set; }

    public int HotelId { get; set; }

    public Hotel Hotel { get; set; }


    // BOOKINGS
    public ICollection<Booking> Bookings { get; set; }
        = new List<Booking>();


    // AMENITIES
    public ICollection<Amenity> Amenities { get; set; }
        = new List<Amenity>();


    // ROOM IMAGES
    public ICollection<RoomImage> Images { get; set; }
        = new List<RoomImage>();
}
