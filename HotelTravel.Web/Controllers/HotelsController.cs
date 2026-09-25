using HotelTravel.Application.DTOs.Hotels;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

public class HotelsController : Controller
{
    private readonly IHotelService _hotelService;

    public HotelsController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    // GET: /Hotels
    public async Task<IActionResult> Index()
    {
        var hotels = await _hotelService.GetAllAsync();

        return View(hotels);
    }

    // GET: /Hotels/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var hotel = await _hotelService.GetByIdAsync(id);

        if (hotel is null)
            return NotFound();

        return View(hotel);
    }

    // GET: /Hotels/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Hotels/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateHotelDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        await _hotelService.CreateAsync(dto);

        return RedirectToAction(nameof(Index));
    }

    // GET: /Hotels/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var hotel = await _hotelService.GetByIdAsync(id);

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

    // POST: /Hotels/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateHotelDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var result = await _hotelService.UpdateAsync(id, dto);

        if (!result)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Hotels/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var hotel = await _hotelService.GetByIdAsync(id);

        if (hotel is null)
            return NotFound();

        return View(hotel);
    }

    // POST: /Hotels/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _hotelService.DeleteAsync(id);

        if (!result)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }
}