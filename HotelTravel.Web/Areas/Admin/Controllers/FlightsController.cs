using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class FlightsController : Controller
    {
        private readonly IFlightService _flightService;
        private readonly IFlightNetworkService _network;
        private readonly IStringLocalizer<HotelTravel.Web.SharedResource> _t;

        public FlightsController(
            IFlightService flightService,
            IFlightNetworkService network,
            IStringLocalizer<HotelTravel.Web.SharedResource> localizer)
        {
            _flightService = flightService;
            _network = network;
            _t = localizer;
        }

        // AviationStack-dan real reys nömrələri və saatları
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportReal()
        {
            var result = await _network.ImportRealAsync();

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Added > 0
                ? _t["{0} real flights were imported.", result.Added].Value
                : result.Message;

            return RedirectToAction(nameof(Index));
        }

        // ROUTES + BOOKINGS
        [HttpGet]
        public async Task<IActionResult> Index(string? city, FlightBookingStatus? status)
        {
            ViewBag.City = city;
            ViewBag.Status = status;
            ViewBag.Cities = AirportCities();
            ViewBag.Bookings = await _flightService.GetBookingsAsync(status);
            ViewBag.RealDataEnabled = await _network.IsRealDataEnabledAsync();

            var routes = await _flightService.GetRoutesAsync(city);

            return View(routes);
        }

        // BOOKING: COMPLETE / CANCEL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingStatus(
            int id,
            FlightBookingStatus status,
            FlightBookingStatus? filter)
        {
            var ok = await _flightService.SetBookingStatusAsync(id, status);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Flight booking updated." : "This flight booking cannot be changed.";

            return RedirectToAction(nameof(Index), new { status = filter });
        }


        private static List<string> AirportCities()
            => TaxiRules.Airports.Select(a => a.City).OrderBy(c => c).ToList();
    }
}
