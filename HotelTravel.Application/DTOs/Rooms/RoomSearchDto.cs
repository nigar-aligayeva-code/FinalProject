namespace HotelTravel.Application.DTOs.Rooms;

public class RoomSearchDto
{
    public DateTime CheckInDate { get; set; }

    public DateTime CheckOutDate { get; set; }

    public int AdultCount { get; set; }

    public int ChildrenCount { get; set; }
}