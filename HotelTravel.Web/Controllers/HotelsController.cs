using HotelTravel.Application.DTOs.Hotels;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

public class HotelsController : Controller
{
    private readonly IHotelService _hotelService;

    public HotelsController(IHotelService hotelService)
    {
        _hotelService = hotelService;
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
    public IActionResult Create()
    {
        return View();
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateHotelDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        await _hotelService.CreateAsync(dto);

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
            return View(dto);

        var result =
            await _hotelService.UpdateAsync(id, dto);

        if (!result)
            return NotFound();

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

        return RedirectToAction(nameof(Index));
    }
}