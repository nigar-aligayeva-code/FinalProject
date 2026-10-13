namespace HotelTravel.Application.DTOs.Reviews;

// Rəy formunu göstərməzdən əvvəl rezervasiya yoxlanılır
public class ReviewEligibilityDto
{
    public bool CanReview { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ConfirmationCode { get; set; } = string.Empty;

    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
}
