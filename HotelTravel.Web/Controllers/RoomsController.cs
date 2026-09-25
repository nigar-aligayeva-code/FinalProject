using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

public class RoomsController : Controller
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    // GET: /Rooms
    public async Task<IActionResult> Index()
    {
        var rooms = await _roomService.GetAllAsync();

        return View(rooms);
    }

    // GET: /Rooms/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var room = await _roomService.GetByIdAsync(id);

        if (room is null)
            return NotFound();

        return View(room);
    }

    // GET: /Rooms/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Rooms/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoomDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        await _roomService.CreateAsync(dto);

        return RedirectToAction(nameof(Index));
    }

    // GET: /Rooms/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _roomService.GetByIdAsync(id);

        if (room is null)
            return NotFound();

        var dto = new UpdateRoomDto
        {
            RoomNumber = room.RoomNumber,
            Name = room.Name,
            Description = room.Description,
            PricePerNight = room.PricePerNight,
            Capacity = room.Capacity,
            MainImage = room.MainImage,
            RoomType = room.RoomType,
            HotelId = room.HotelId
        };

        return View(dto);
    }

    // POST: /Rooms/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateRoomDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var result = await _roomService.UpdateAsync(id, dto);

        if (!result)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Rooms/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _roomService.GetByIdAsync(id);

        if (room is null)
            return NotFound();

        return View(room);
    }

    // POST: /Rooms/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _roomService.DeleteAsync(id);

        if (!result)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }
}