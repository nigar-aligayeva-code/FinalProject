namespace HotelTravel.Application.DTOs.Reviews;

public class GetReviewDto
{
    public int Id { get; set; }

    public int Rating { get; set; }

    public string Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    // Məxfilik üçün: "Nigar A." formasında
    public string GuestDisplayName { get; set; }

    public int RoomId { get; set; }
    public string RoomName { get; set; }
    public string RoomNumber { get; set; }

    public int BookingId { get; set; }
    public string ConfirmationCode { get; set; }

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
}
