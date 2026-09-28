namespace HotelTravel.Application.DTOs.Bookings;

public class OccupancyCalendarDto
{
    public int BookingId { get; set; }

    public int RoomId { get; set; }

    public string RoomNumber { get; set; }

    public string RoomName { get; set; }

    public DateTime CheckInDate { get; set; }

    public DateTime CheckOutDate { get; set; }

    public string GuestName { get; set; }

    public string ConfirmationCode { get; set; }

    public string Status { get; set; }
}