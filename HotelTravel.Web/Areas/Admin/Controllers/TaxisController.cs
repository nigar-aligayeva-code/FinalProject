using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TaxisController : Controller
    {
        private readonly ITaxiService _taxiService;

        public TaxisController(ITaxiService taxiService)
        {
            _taxiService = taxiService;
        }

        // ALL TAXI BOOKINGS
        [HttpGet]
        public async Task<IActionResult> Index(TaxiBookingStatus? status)
        {
            ViewBag.Status = status;

            var bookings = await _taxiService.GetAllAsync(status);

            return View(bookings);
        }

        // COMPLETE / CANCEL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(
            int id,
            TaxiBookingStatus status,
            TaxiBookingStatus? filter)
        {
            var ok = await _taxiService.SetStatusAsync(id, status);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Taxi booking updated." : "This taxi booking cannot be changed.";

            return RedirectToAction(nameof(Index), new { status = filter });
        }
    }
}
