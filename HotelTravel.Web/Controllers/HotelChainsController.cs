using HotelTravel.Application.DTOs.HotelChains;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

[Authorize(Roles = "Admin")]
public class HotelChainsController : Controller
{
    private readonly IHotelChainService _hotelChainService;

    public HotelChainsController(
        IHotelChainService hotelChainService)
    {
        _hotelChainService = hotelChainService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var hotelChains =
            await _hotelChainService.GetAllAsync();

        return View(hotelChains);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var hotelChain =
            await _hotelChainService.GetByIdAsync(id);

        if (hotelChain is null)
            return NotFound();

        return View(hotelChain);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateHotelChainDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        await _hotelChainService.CreateAsync(dto);

        TempData["SuccessMessage"] =
            "Hotel chain created successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var hotelChain =
            await _hotelChainService.GetByIdAsync(id);

        if (hotelChain is null)
            return NotFound();

        var dto = new UpdateHotelChainDto
        {
            Name = hotelChain.Name,
            Description = hotelChain.Description,
            Logo = hotelChain.Logo
        };

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        UpdateHotelChainDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var result =
            await _hotelChainService.UpdateAsync(
                id,
                dto);

        if (!result)
            return NotFound();

        TempData["SuccessMessage"] =
            "Hotel chain updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await _hotelChainService.DeleteAsync(id);

        if (!result)
            return NotFound();

        TempData["SuccessMessage"] =
            "Hotel chain deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
}