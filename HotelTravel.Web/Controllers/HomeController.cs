using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using HotelTravel.Web.Models;
using HotelTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HotelTravel.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRoomService _roomService;
        private readonly IHotelService _hotelService;
        private readonly CurrentUserAccessor _currentUser;

        public HomeController(
            ILogger<HomeController> logger,
            IRoomService roomService,
            IHotelService hotelService,
            CurrentUserAccessor currentUser)
        {
            _logger = logger;
            _roomService = roomService;
            _hotelService = hotelService;
            _currentUser = currentUser;
        }


        // =========================================
        // HOME
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? destination,
            DateTime? checkInDate,
            DateTime? checkOutDate,
            int? adultCount,
            int? childrenCount)
        {
            var model = new RoomSearchDto
            {
                Destination = destination,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                AdultCount = adultCount ?? 1,
                ChildrenCount = childrenCount ?? 0
            };

            await LoadHomeSectionsAsync();

            return View(model);
        }


        // =========================================
        // SEARCH
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(
            RoomSearchDto search)
        {
            // Check-in keçmiş tarix ola bilməz
            if (search.CheckInDate.HasValue &&
                search.CheckInDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(search.CheckInDate),
                    "Check-in date cannot be in the past.");
            }


            // Check-out check-in-dən sonra olmalıdır
            if (search.CheckInDate.HasValue &&
                search.CheckOutDate.HasValue &&
                search.CheckInDate.Value.Date >=
                search.CheckOutDate.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(search.CheckOutDate),
                    "Check-out date must be after check-in date.");
            }


            // Validation problemi varsa Home-a qayıt
            if (!ModelState.IsValid)
            {
                await LoadHomeSectionsAsync();

                return View("Index", search);
            }


            // =====================================
            // FIND AVAILABLE ROOMS
            // =====================================

            var rooms =
                await _roomService
                    .SearchAvailableRoomsAsync(search);


            // =====================================
            // KEEP SEARCH INFORMATION
            // =====================================

            ViewBag.Destination =
                search.Destination;

            ViewBag.CheckInDate =
                search.CheckInDate!.Value;

            ViewBag.CheckOutDate =
                search.CheckOutDate!.Value;

            ViewBag.AdultCount =
                search.AdultCount;

            ViewBag.ChildrenCount =
                search.ChildrenCount;


            // =====================================
            // RESULTS
            // =====================================

            return View(
                "~/Views/Rooms/AvailableRooms.cshtml",
                rooms);
        }


        // =========================================
        // FLIGHTS / CAR RENTAL / ATTRACTIONS / TAXIS
        // Hələlik yalnız otel rezervasiyası işləyir,
        // digər xidmətlər üçün "Coming soon" səhifəsi
        // =========================================

        [HttpGet]
        public IActionResult ComingSoon(string? service)
        {
            var allowed = new[] { "flights", "cars", "attractions", "taxis" };

            if (string.IsNullOrWhiteSpace(service) ||
                !allowed.Contains(service))
            {
                return RedirectToAction(nameof(Index));
            }

            // Layout-da uyğun tab aktiv görünsün
            ViewData["Service"] = service;

            return View(model: service);
        }


        // =========================================
        // HOME PAGE SECTIONS
        // =========================================

        private async Task LoadHomeSectionsAsync()
        {
            ViewBag.Destinations =
                await _hotelService.GetPopularDestinationsAsync(8);

            ViewBag.FeaturedRooms =
                await _roomService.GetFeaturedRoomsAsync(4);

            // "Nigar, where to next?" salamlaşması üçün
            var user = await _currentUser.GetUserAsync(User);

            ViewBag.FirstName = user?.FirstName;
        }


        // =========================================
        // PRIVACY
        // =========================================

        public IActionResult Privacy()
        {
            return View();
        }


        // =========================================
        // ERROR
        // =========================================

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                });
        }
    }
}