using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class FlightsController : Controller
    {
        private readonly IFlightService _flightService;

        public FlightsController(IFlightService flightService)
        {
            _flightService = flightService;
        }

        // ROUTES + BOOKINGS
        [HttpGet]
        public async Task<IActionResult> Index(string? city, FlightBookingStatus? status)
        {
            ViewBag.City = city;
            ViewBag.Status = status;
            ViewBag.Cities = AirportCities();
            ViewBag.Bookings = await _flightService.GetBookingsAsync(status);

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
