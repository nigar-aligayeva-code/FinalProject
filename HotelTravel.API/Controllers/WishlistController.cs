using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

// Saved rooms (yalnız daxil olmuş istifadəçi)
/// <summary>Saved rooms (wishlist) of the signed-in user.</summary>
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
    /// <summary>Saved rooms of the signed-in user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<FeaturedRoomDto>), 200)]
    public async Task<IActionResult> GetSaved()
        => Ok(await _wishlistService.GetSavedRoomsAsync(CurrentUserId!));

    // GET: api/wishlist/ids
    /// <summary>Ids of saved rooms (to fill the heart icons).</summary>
    [HttpGet("ids")]
    [ProducesResponseType(typeof(List<int>), 200)]
    public async Task<IActionResult> GetIds()
        => Ok((await _wishlistService.GetSavedRoomIdsAsync(CurrentUserId)).ToList());

    // POST: api/wishlist/toggle/5   -> Flag = indi saved-dir?
    /// <summary>Save or unsave a room. Flag in the response is true when the room is now saved.</summary>
    [HttpPost("toggle/{roomId:int}")]
    [HotelTravel.API.Filters.GuestOnly]
    public async Task<IActionResult> Toggle(int roomId)
    {
        var result = await _wishlistService.ToggleAsync(CurrentUserId!, roomId);

        return result.Success
            ? Ok(ApiResultDto.Ok(result.Saved ? "Saved." : "Removed.", flag: result.Saved))
            : NotFound(new ApiResultDto { Success = false, Message = "Room not found.", Flag = result.Saved });
    }
}
