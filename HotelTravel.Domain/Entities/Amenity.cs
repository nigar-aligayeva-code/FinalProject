namespace HotelTravel.Domain.Entities;

public class Amenity : BaseEntity
{
    public string Name { get; set; }

    public string Icon { get; set; }

    public ICollection<Room> Rooms { get; set; }
        = new List<Room>();
}