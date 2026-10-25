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

        // Bütün otel şəhərləri arasında çatışmayan reysləri avtomatik yarat
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate()
        {
            var result = await _network.GenerateMissingAsync();

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Added > 0
                ? _t["{0} new daily flights were created automatically.", result.Added].Value
                : result.Message;

            return RedirectToAction(nameof(Index));
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

        // ADD / EDIT
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            SaveFlightRouteDto? dto;

            if (id.HasValue)
            {
                dto = await _flightService.GetRouteForEditAsync(id.Value);

                if (dto is null)
                    return NotFound();
            }
            else
            {
                dto = new SaveFlightRouteDto();
            }

            ViewBag.Cities = AirportCities();

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SaveFlightRouteDto dto)
        {
            if (ModelState.IsValid)
            {
                var result = await _flightService.SaveRouteAsync(dto);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, result.Message);
            }

            ViewBag.Cities = AirportCities();

            return View(dto);
        }

        // ACTIVE / INACTIVE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, string? city)
        {
            var ok = await _flightService.ToggleRouteAsync(id);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Flight updated." : "Flight was not found.";

            return RedirectToAction(nameof(Index), new { city });
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
