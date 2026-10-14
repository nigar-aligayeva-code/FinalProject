using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly IWishlistService _wishlistService;
    private readonly UserManager<AppUser> _userManager;

    public WishlistController(
        IWishlistService wishlistService,
        UserManager<AppUser> userManager)
    {
        _wishlistService = wishlistService;
        _userManager = userManager;
    }


    // =========================
    // SAVED ROOMS PAGE
    // =========================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rooms =
            await _wishlistService.GetSavedRoomsAsync(
                _userManager.GetUserId(User)!);

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
                _userManager.GetUserId(User)!,
                roomId);

        if (!result.Success)
            return NotFound();

        return Json(new { saved = result.Saved });
    }
}
