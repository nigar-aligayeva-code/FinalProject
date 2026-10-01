using HotelTravel.Application.DTOs.Hotels;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

public class HotelsController : Controller
{
    private readonly IHotelService _hotelService;
    private readonly IHotelChainService _hotelChainService;

    public HotelsController(
        IHotelService hotelService,
        IHotelChainService hotelChainService)
    {
        _hotelService = hotelService;
        _hotelChainService = hotelChainService;
    }


    // =========================
    // PUBLIC - HOTEL LIST
    // =========================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var hotels =
            await _hotelService.GetAllAsync();

        return View(hotels);
    }


    // =========================
    // PUBLIC - HOTEL DETAILS
    // =========================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var hotel =
            await _hotelService.GetByIdAsync(id);

        if (hotel is null)
            return NotFound();

        return View(hotel);
    }


    // =========================
    // ADMIN - CREATE HOTEL
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadHotelChainsAsync();

        return View();
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateHotelDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadHotelChainsAsync();

            return View(dto);
        }

        await _hotelService.CreateAsync(dto);

        TempData["SuccessMessage"] =
            "Hotel created successfully.";

        return RedirectToAction(nameof(Index));
    }


    // =========================
    // ADMIN - EDIT HOTEL
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var hotel =
            await _hotelService.GetByIdAsync(id);

        if (hotel is null)
            return NotFound();

        var dto = new UpdateHotelDto
        {
            Name = hotel.Name,
            Description = hotel.Description,
            Address = hotel.Address,
            City = hotel.City,
            Country = hotel.Country,
            Phone = hotel.Phone,
            Email = hotel.Email,
            MainImage = hotel.MainImage,
            Latitude = hotel.Latitude,
            Longitude = hotel.Longitude,
            StarRating = hotel.StarRating,
            HotelChainId = hotel.HotelChainId
        };

        await LoadHotelChainsAsync();

        return View(dto);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        UpdateHotelDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadHotelChainsAsync();

            return View(dto);
        }

        var result =
            await _hotelService.UpdateAsync(
                id,
                dto);

        if (!result)
            return NotFound();

        TempData["SuccessMessage"] =
            "Hotel updated successfully.";

        return RedirectToAction(nameof(Index));
    }


    // =========================
    // ADMIN - DELETE HOTEL
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var hotel =
            await _hotelService.GetByIdAsync(id);

        if (hotel is null)
            return NotFound();

        return View(hotel);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result =
            await _hotelService.DeleteAsync(id);

        if (!result)
            return NotFound();

        TempData["SuccessMessage"] =
            "Hotel deleted successfully.";

        return RedirectToAction(nameof(Index));
    }


    // =========================
    // LOAD HOTEL CHAINS
    // =========================
    private async Task LoadHotelChainsAsync()
    {
        ViewBag.HotelChains =
            await _hotelChainService.GetAllAsync();
    }
}