namespace HotelTravel.Domain.Entities;

public class HotelChain : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Logo { get; set; }

    public ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
}