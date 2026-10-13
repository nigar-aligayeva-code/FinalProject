using HotelTravel.Application.DTOs.Reviews;

namespace HotelTravel.Application.Interfaces;

public interface IReviewService
{
    // Rezervasiya rəy yazmağa uyğundurmu?
    Task<ReviewEligibilityDto> CheckEligibilityAsync(
        string confirmationCode);

    Task<(bool Success, string Message, int? RoomId)>
        CreateAsync(CreateReviewDto dto);

    // Otaq səhifəsi üçün
    Task<List<GetReviewDto>> GetByRoomIdAsync(int roomId);

    Task<RoomRatingSummaryDto> GetRoomSummaryAsync(int roomId);

    // Admin
    Task<List<GetReviewDto>> GetAllAsync();

    Task<bool> DeleteAsync(int id);
}
