namespace HotelTravel.Application.DTOs.Reviews;

/// <summary>
/// Otelin reytinqi: qonaqların otelə verdiyi qiymət (xidmət, yer, təmizlik)
/// və otellərin otaqlarına yazılmış rəylərin ümumi sayı.
/// </summary>
public class HotelRatingSummaryDto
{
    public int HotelId { get; set; }

    // Otelə verilən qiymətlər
    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }

    // Key: ulduz (1-5), Value: rəy sayı
    public Dictionary<int, int> StarCounts { get; set; } = new();

    // Otelin otaqlarına yazılmış rəylər (müqayisə üçün)
    public double RoomAverageRating { get; set; }

    public int RoomReviewCount { get; set; }
}
