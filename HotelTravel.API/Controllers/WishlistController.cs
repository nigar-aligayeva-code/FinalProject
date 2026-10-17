using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

// Saved rooms (yalnız daxil olmuş istifadəçi)
[Route("api/wishlist")]
[Authorize]
public class WishlistController : ApiControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    // GET: api/wishlist
    [HttpGet]
    [ProducesResponseType(typeof(List<FeaturedRoomDto>), 200)]
    public async Task<IActionResult> GetSaved()
        => Ok(await _wishlistService.GetSavedRoomsAsync(CurrentUserId!));

    // GET: api/wishlist/ids
    [HttpGet("ids")]
    [ProducesResponseType(typeof(List<int>), 200)]
    public async Task<IActionResult> GetIds()
        => Ok((await _wishlistService.GetSavedRoomIdsAsync(CurrentUserId)).ToList());

    // POST: api/wishlist/toggle/5   -> Flag = indi saved-dir?
    [HttpPost("toggle/{roomId:int}")]
    public async Task<IActionResult> Toggle(int roomId)
    {
        var result = await _wishlistService.ToggleAsync(CurrentUserId!, roomId);

        return result.Success
            ? Ok(ApiResultDto.Ok(result.Saved ? "Saved." : "Removed.", flag: result.Saved))
            : NotFound(new ApiResultDto { Success = false, Message = "Room not found.", Flag = result.Saved });
    }
}
