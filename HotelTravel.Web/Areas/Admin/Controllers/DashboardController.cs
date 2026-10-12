
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IHotelService _hotelService;
        private readonly IRoomService _roomService;
        private readonly IBookingService _bookingService;

        public DashboardController(
            IHotelService hotelService,
            IRoomService roomService,
            IBookingService bookingService)
        {
            _hotelService = hotelService;
            _roomService = roomService;
            _bookingService = bookingService;
        }

        public async Task<IActionResult> Index()
        {
            var hotels = await _hotelService.GetAllAsync();
            var rooms = await _roomService.GetAllAsync();
            var bookings = await _bookingService.GetAllAsync();

            // STATISTICS
            ViewBag.TotalHotels = hotels.Count;
            ViewBag.TotalRooms = rooms.Count;
            ViewBag.TotalBookings = bookings.Count;

            // Revenue excludes cancelled bookings
            var validBookings = bookings
                .Where(b => b.Status != BookingStatus.Cancelled)
                .ToList();

            ViewBag.TotalRevenue = validBookings
                .Sum(b => b.TotalPrice);

            // MONTHLY BOOKINGS - CURRENT YEAR
            int year = DateTime.Today.Year;

            ViewBag.MonthlyBookings = Enumerable
                .Range(1, 12)
                .Select(month => bookings.Count(b =>
                    b.CheckInDate.Year == year &&
                    b.CheckInDate.Month == month))
                .ToArray();

            // MONTHLY REVENUE - CURRENT YEAR
            ViewBag.MonthlyRevenue = Enumerable
                .Range(1, 12)
                .Select(month => validBookings
                    .Where(b =>
                        b.CheckInDate.Year == year &&
                        b.CheckInDate.Month == month)
                    .Sum(b => b.TotalPrice))
                .ToArray();

            // BOOKING STATUS
            ViewBag.BookingStatuses = Enum
                .GetValues<BookingStatus>()
                .Select(s => s.ToString())
                .ToArray();

            ViewBag.BookingStatusCounts = Enum
                .GetValues<BookingStatus>()
                .Select(s => bookings.Count(b => b.Status == s))
                .ToArray();

            ViewBag.ChartYear = year;

            return View();
        }
    }
}
