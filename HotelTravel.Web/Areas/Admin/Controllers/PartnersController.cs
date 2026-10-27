using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.Interfaces;
using HotelTravel.Web.Services.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers;

// Admin: otel partnyorlarının hesabları (otel işçiləri)
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PartnersController : Controller
{
    private readonly IPartnerApiClient _partners;
    private readonly IHotelService _hotels;

    public PartnersController(IPartnerApiClient partners, IHotelService hotels)
    {
        _partners = partners;
        _hotels = hotels;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await LoadAsync();
        return View(new CreatePartnerDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePartnerDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return View(nameof(Index), dto);
        }

        var result = await _partners.CreateAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await LoadAsync();
            return View(nameof(Index), dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string userId)
    {
        var result = await _partners.RemoveAsync(userId);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadAsync()
    {
        ViewBag.Partners = await _partners.GetAllAsync();
        ViewBag.Hotels = (await _hotels.GetAllAsync()).OrderBy(h => h.Name).ToList();
    }
}
