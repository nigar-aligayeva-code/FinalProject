
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // ALL BOOKINGS
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            BookingStatus? status)
        {
            var bookings = await _bookingService.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                bookings = bookings.Where(b =>
                    b.ConfirmationCode.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) ||
                    b.GuestName.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) ||
                    b.GuestEmail.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) ||
                    b.RoomNumber.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            if (status.HasValue)
            {
                bookings = bookings
                    .Where(b => b.Status == status.Value)
                    .ToList();
            }

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(
                bookings.OrderByDescending(b => b.CheckInDate).ToList()
            );
        }

        // BOOKING DETAILS
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _bookingService.GetByIdAsync(id);

            if (booking == null)
                return NotFound();

            return View(booking);
        }

        // CHANGE BOOKING STATUS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(
            int id,
            BookingStatus status)
        {
            if (!Enum.IsDefined(typeof(BookingStatus), status))
            {
                TempData["Error"] = "Invalid booking status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var updated = await _bookingService.UpdateStatusAsync(
                id,
                status);

            if (!updated)
            {
                TempData["Error"] =
                    "Status could not be changed. Check the booking status and dates.";
            }
            else
            {
                TempData["Success"] =
                    "Booking status updated successfully.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Calendar()
        {
            var bookings =
                await _bookingService.GetOccupancyCalendarAsync();

            return View(bookings);
        }

    }

}
