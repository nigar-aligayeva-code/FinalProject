using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using HotelTravel.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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


    // =========================
    // ADMIN - ALL BOOKINGS
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var bookings =
            await _bookingService.GetAllAsync();

        return View(bookings);
    }


    // =========================
    // ADMIN - BOOKING DETAILS
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var booking =
            await _bookingService.GetByIdAsync(id);

        if (booking is null)
            return NotFound();

        return View(booking);
    }


    // =========================
    // PUBLIC - FIND MY BOOKING
    // =========================
    [HttpGet]
    public IActionResult Find()
    {
        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Find(
        string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
        {
            ViewBag.ErrorMessage =
                "Please enter your confirmation code.";

            return View();
        }

        var booking =
            await _bookingService
                .GetByConfirmationCodeAsync(
                    confirmationCode);

        if (booking is null)
        {
            ViewBag.ErrorMessage =
                "Booking was not found. Please check your confirmation code.";

            return View();
        }

        return View("FindResult", booking);
    }


    // =========================
    // ADMIN - OCCUPANCY CALENDAR
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Calendar(
        int? year,
        int? month)
    {
        var today = DateTime.Today;

        int selectedYear =
            year ?? today.Year;

        int selectedMonth =
            month ?? today.Month;

        if (selectedMonth < 1 ||
            selectedMonth > 12)
        {
            selectedYear = today.Year;
            selectedMonth = today.Month;
        }

        var rooms =
            await _roomService.GetAllAsync();

        var bookings =
            await _bookingService
                .GetOccupancyCalendarAsync();

        var viewModel =
            new OccupancyCalendarViewModel
            {
                Year = selectedYear,
                Month = selectedMonth,
                Rooms = rooms,
                Bookings = bookings
            };

        return View(viewModel);
    }


    // =========================
    // ADMIN - UPDATE STATUS
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        BookingStatus status)
    {
        var result =
            await _bookingService
                .UpdateStatusAsync(id, status);

        if (!result)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }


    // =========================
    // PUBLIC - CREATE BOOKING
    // =========================
    [HttpGet]
    public async Task<IActionResult> Create(
        int? roomId,
        DateTime? checkInDate,
        DateTime? checkOutDate,
        int? adultCount,
        int? childrenCount)
    {
        // Search Rooms -> Book Now
        if (roomId.HasValue &&
            checkInDate.HasValue &&
            checkOutDate.HasValue)
        {
            await LoadRoomsAsync(roomId);

            var dto = new CreateBookingDto
            {
                RoomId = roomId.Value,
                CheckInDate = checkInDate.Value,
                CheckOutDate = checkOutDate.Value,
                AdultCount = adultCount ?? 1,
                ChildrenCount = childrenCount ?? 0
            };

            ViewBag.FromSearch = true;

            return View(dto);
        }

        // Otaq və tarix seçilməyibsə
        // əvvəl Search Rooms-a getsin
        return RedirectToAction(
            "Search",
            "Rooms");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateBookingDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadRoomsAsync(dto.RoomId);

            return View(dto);
        }

        var result =
            await _bookingService.CreateAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError(
                string.Empty,
                result.Message);

            await LoadRoomsAsync(dto.RoomId);

            return View(dto);
        }

        TempData["SuccessMessage"] =
            $"Booking created successfully. Confirmation Code: {result.ConfirmationCode}";

        return RedirectToAction(
            nameof(Find),
            new
            {
                confirmationCode =
                    result.ConfirmationCode
            });
    }


    // =========================
    // PRIVATE HELPER
    // =========================
    private async Task LoadRoomsAsync(
        int? selectedRoomId = null)
    {
        var rooms =
            await _roomService.GetAllAsync();

        var roomItems =
            rooms.Select(r => new
            {
                r.Id,

                DisplayName =
                    $"{r.RoomNumber} - {r.Name} - {r.PricePerNight:0.00} AZN"
            });

        ViewBag.Rooms =
            new SelectList(
                roomItems,
                "Id",
                "DisplayName",
                selectedRoomId);
    }
}