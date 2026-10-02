using HotelTravel.Application.DTOs.Amenities;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AmenitiesController : Controller
{
    private readonly IAmenityService _amenityService;

    public AmenitiesController(
        IAmenityService amenityService)
    {
        _amenityService = amenityService;
    }

    public async Task<IActionResult> Index()
    {
        var amenities =
            await _amenityService.GetAllAsync();

        return View(amenities);
    }

    public async Task<IActionResult> Details(int id)
    {
        var amenity =
            await _amenityService.GetByIdAsync(id);

        if (amenity is null)
            return NotFound();

        return View(amenity);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateAmenityDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        await _amenityService.CreateAsync(dto);

        TempData["SuccessMessage"] =
            "Amenity created successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var amenity =
            await _amenityService.GetByIdAsync(id);

        if (amenity is null)
            return NotFound();

        var dto = new UpdateAmenityDto
        {
            Name = amenity.Name,
            Icon = amenity.Icon
        };

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        UpdateAmenityDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var result =
            await _amenityService.UpdateAsync(id, dto);

        if (!result)
            return NotFound();

        TempData["SuccessMessage"] =
            "Amenity updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await _amenityService.DeleteAsync(id);

        if (!result.Success)
        {
            TempData["ErrorMessage"] =
                result.Message;

            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] =
            result.Message;

        return RedirectToAction(nameof(Index));
    }
}