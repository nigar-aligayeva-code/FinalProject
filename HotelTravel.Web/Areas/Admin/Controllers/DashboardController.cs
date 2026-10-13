
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

            // EXISTING STATISTICS
            ViewBag.TotalHotels = hotels.Count;
            ViewBag.TotalRooms = rooms.Count;
            ViewBag.TotalBookings = bookings.Count;

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

            // NEW: TODAY'S CHECK-INS
            var today = DateTime.Today;

            ViewBag.TodayCheckIns = validBookings.Count(b =>
                b.CheckInDate.Date == today);

            // NEW: TODAY'S CHECK-OUTS
            ViewBag.TodayCheckOuts = validBookings.Count(b =>
                b.CheckOutDate.Date == today);

            // NEW: MONTHLY OCCUPANCY RATE
            var monthStart = new DateTime(
                today.Year,
                today.Month,
                1);

            var monthEnd = monthStart.AddMonths(1);

            int daysInMonth = DateTime.DaysInMonth(
                today.Year,
                today.Month);

            var activeRoomIds = rooms
                .Select(r => r.Id)
                .ToHashSet();

            var occupiedRoomDays = new HashSet<(int RoomId, DateTime Day)>();

            foreach (var booking in validBookings)
            {
                if (!activeRoomIds.Contains(booking.RoomId))
                    continue;

                var start = booking.CheckInDate.Date > monthStart
                    ? booking.CheckInDate.Date
                    : monthStart;

                var end = booking.CheckOutDate.Date < monthEnd
                    ? booking.CheckOutDate.Date
                    : monthEnd;

                for (var day = start; day < end; day = day.AddDays(1))
                {
                    occupiedRoomDays.Add((booking.RoomId, day));
                }
            }

            int totalRoomNights = rooms.Count * daysInMonth;

            ViewBag.MonthlyOccupancyRate = totalRoomNights > 0
                ? Math.Round(
                    (decimal)occupiedRoomDays.Count /
                    totalRoomNights * 100,
                    1)
                : 0m;
            // RECENT BOOKINGS
            ViewBag.RecentBookings = bookings
                .OrderByDescending(b => b.CheckInDate)
                .Take(5)
                .ToList();

            return View();
        }
    }
}
