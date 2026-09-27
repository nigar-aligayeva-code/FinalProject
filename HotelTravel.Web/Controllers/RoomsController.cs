using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelTravel.Web.Controllers;

public class RoomsController : Controller
{
    private readonly IRoomService _roomService;
    private readonly IHotelService _hotelService;

    public RoomsController(
        IRoomService roomService,
        IHotelService hotelService)
    {
        _roomService = roomService;
        _hotelService = hotelService;
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
    public async Task<IActionResult> Create()
    {
        await LoadHotelsAsync();

        return View();
    }

    // POST: /Rooms/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoomDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadHotelsAsync(dto.HotelId);

            return View(dto);
        }

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

        await LoadHotelsAsync(dto.HotelId);

        return View(dto);
    }

    // POST: /Rooms/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateRoomDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadHotelsAsync(dto.HotelId);

            return View(dto);
        }

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
    [HttpGet]
    public IActionResult Search()
    {
        return View(new RoomSearchDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(RoomSearchDto search)
    {
        if (search.CheckInDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(
                string.Empty,
                "Check-in date cannot be in the past.");

            return View(search);
        }

        if (search.CheckInDate.Date >= search.CheckOutDate.Date)
        {
            ModelState.AddModelError(
                string.Empty,
                "Check-out date must be after check-in date.");

            return View(search);
        }

        if (search.AdultCount <= 0 || search.ChildrenCount < 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Guest count is not valid.");

            return View(search);
        }

        var rooms =
            await _roomService.SearchAvailableRoomsAsync(search);

        ViewBag.CheckInDate = search.CheckInDate;
        ViewBag.CheckOutDate = search.CheckOutDate;
        ViewBag.AdultCount = search.AdultCount;
        ViewBag.ChildrenCount = search.ChildrenCount;

        return View("AvailableRooms", rooms);
    }
    private async Task LoadHotelsAsync(int? selectedHotelId = null)
    {
        var hotels = await _hotelService.GetAllAsync();

        ViewBag.Hotels = new SelectList(
            hotels,
            "Id",
            "Name",
            selectedHotelId);
    }
}