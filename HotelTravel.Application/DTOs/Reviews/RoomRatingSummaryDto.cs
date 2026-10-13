namespace HotelTravel.Application.DTOs.Reviews;

public class RoomRatingSummaryDto
{
    public int RoomId { get; set; }

    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }

    // Key: ulduz (1-5), Value: rəy sayı
    public Dictionary<int, int> StarCounts { get; set; } = new();
}
