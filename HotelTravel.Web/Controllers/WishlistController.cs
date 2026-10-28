using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using HotelTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

[Authorize]
[HotelTravel.Web.Filters.GuestOnly]
public class WishlistController : Controller
{
    private readonly IWishlistService _wishlistService;
    private readonly CurrentUserAccessor _currentUser;

    public WishlistController(
        IWishlistService wishlistService,
        CurrentUserAccessor currentUser)
    {
        _wishlistService = wishlistService;
        _currentUser = currentUser;
    }


    // =========================
    // SAVED ROOMS PAGE
    // =========================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rooms =
            await _wishlistService.GetSavedRoomsAsync(
                _currentUser.GetUserId(User)!);

        return View(rooms);
    }


    // =========================
    // TOGGLE (AJAX - ürək düyməsi)
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int roomId)
    {
        var result =
            await _wishlistService.ToggleAsync(
                _currentUser.GetUserId(User)!,
                roomId);

        if (!result.Success)
            return NotFound();

        return Json(new { saved = result.Saved });
    }
}
