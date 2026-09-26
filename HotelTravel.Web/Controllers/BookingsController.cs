using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelTravel.Domain.Enums;
namespace HotelTravel.Web.Controllers;

public class BookingsController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IRoomService _roomService;

    public BookingsController(
        IBookingService bookingService,
        IRoomService roomService)
    {
        _bookingService = bookingService;
        _roomService = roomService;
    }

    // GET: /Bookings
    public async Task<IActionResult> Index()
    {
        var bookings = await _bookingService.GetAllAsync();

        return View(bookings);
    }

    // GET: /Bookings/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var booking = await _bookingService.GetByIdAsync(id);

        if (booking is null)
            return NotFound();

        return View(booking);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
    int id,
    BookingStatus status)
    {
        var result =
            await _bookingService.UpdateStatusAsync(id, status);

        if (!result)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }
    // GET: /Bookings/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadRoomsAsync();

        return View();
    }

    // POST: /Bookings/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadRoomsAsync(dto.RoomId);
            return View(dto);
        }

        var result = await _bookingService.CreateAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            await LoadRoomsAsync(dto.RoomId);

            return View(dto);
        }

        TempData["SuccessMessage"] =
            $"Booking created successfully. Confirmation Code: {result.ConfirmationCode}";

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadRoomsAsync(int? selectedRoomId = null)
    {
        var rooms = await _roomService.GetAllAsync();

        var roomItems = rooms.Select(r => new
        {
            r.Id,
            DisplayName =
                $"{r.RoomNumber} - {r.Name} - {r.PricePerNight:0.00} AZN"
        });

        ViewBag.Rooms = new SelectList(
            roomItems,
            "Id",
            "DisplayName",
            selectedRoomId);
    }
}