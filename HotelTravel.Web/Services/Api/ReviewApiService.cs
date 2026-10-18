using HotelTravel.Application.DTOs.Reviews;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Web.Services.Api;

// IReviewService -> API (api/reviews)
public class ReviewApiService : IReviewService
{
    private readonly ApiClient _api;

    public ReviewApiService(ApiClient api)
    {
        _api = api;
    }

    public async Task<ReviewEligibilityDto> CheckEligibilityAsync(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
        {
            return new ReviewEligibilityDto
            {
                CanReview = false,
                Message = "Please enter your confirmation code."
            };
        }

        return await _api.GetAsync<ReviewEligibilityDto>(
            $"api/reviews/eligibility/{ApiUrl.Segment(confirmationCode)}");
    }

    public async Task<(bool Success, string Message, int? RoomId)> CreateAsync(CreateReviewDto dto)
    {
        var result = await _api.PostAsync("api/reviews", dto);
        return (result.Success, result.Message, result.Id);
    }

    public Task<List<GetReviewDto>> GetByRoomIdAsync(int roomId)
        => _api.GetAsync<List<GetReviewDto>>($"api/reviews/room/{roomId}");

    public Task<RoomRatingSummaryDto> GetRoomSummaryAsync(int roomId)
        => _api.GetAsync<RoomRatingSummaryDto>($"api/reviews/room/{roomId}/summary");

    public Task<List<GetReviewDto>> GetAllAsync()
        => _api.GetAsync<List<GetReviewDto>>("api/reviews");

    public async Task<bool> DeleteAsync(int id)
        => (await _api.DeleteAsync($"api/reviews/{id}")).Success;
}
