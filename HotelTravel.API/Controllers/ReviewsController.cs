using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Reviews;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

[Route("api/reviews")]
public class ReviewsController : ApiControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    // GET: api/reviews/eligibility/BK-1234ABCD
    [HttpGet("eligibility/{confirmationCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ReviewEligibilityDto), 200)]
    public async Task<IActionResult> Eligibility(string confirmationCode)
        => Ok(await _reviewService.CheckEligibilityAsync(confirmationCode));

    // POST: api/reviews
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        var result = await _reviewService.CreateAsync(dto);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message, id: result.RoomId)
            : new ApiResultDto { Success = false, Message = result.Message, Id = result.RoomId });
    }

    // GET: api/reviews/room/5
    [HttpGet("room/{roomId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GetReviewDto>), 200)]
    public async Task<IActionResult> ByRoom(int roomId)
        => Ok(await _reviewService.GetByRoomIdAsync(roomId));

    // GET: api/reviews/room/5/summary
    [HttpGet("room/{roomId:int}/summary")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RoomRatingSummaryDto), 200)]
    public async Task<IActionResult> RoomSummary(int roomId)
        => Ok(await _reviewService.GetRoomSummaryAsync(roomId));

    // GET: api/reviews
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<GetReviewDto>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _reviewService.GetAllAsync());

    // DELETE: api/reviews/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
        => FromBool(await _reviewService.DeleteAsync(id), "Review deleted.", "Review not found.");
}
