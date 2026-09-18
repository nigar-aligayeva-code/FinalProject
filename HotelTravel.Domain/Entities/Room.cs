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

    public int HotelId { get; set; }
    public Hotel Hotel { get; set; }
    public RoomType RoomType { get; set; }
}