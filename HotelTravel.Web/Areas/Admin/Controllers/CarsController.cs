using HotelTravel.Application.DTOs.Cars;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CarsController : Controller
    {
        private readonly ICarRentalService _carRentalService;
        private readonly IHotelService _hotelService;

        public CarsController(
            ICarRentalService carRentalService,
            IHotelService hotelService)
        {
            _carRentalService = carRentalService;
            _hotelService = hotelService;
        }

        // FLEET + RENTALS
        [HttpGet]
        public async Task<IActionResult> Index(string? city)
        {
            ViewBag.City = city;
            ViewBag.Cities = await GetHotelCitiesAsync();
            ViewBag.Rentals = await _carRentalService.GetRentalsAsync();

            var fleet = await _carRentalService.GetFleetAsync(city);

            return View(fleet);
        }

        // ADD / EDIT
        [HttpGet]
        public async Task<IActionResult> Edit(int? id, string? city)
        {
            SaveCarDto? dto;

            if (id.HasValue)
            {
                dto = await _carRentalService.GetCarForEditAsync(id.Value);

                if (dto is null)
                    return NotFound();
            }
            else
            {
                dto = new SaveCarDto { City = city ?? string.Empty };
            }

            ViewBag.Cities = await GetHotelCitiesAsync();

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SaveCarDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Cities = await GetHotelCitiesAsync();
                return View(dto);
            }

            await _carRentalService.SaveCarAsync(dto);

            TempData["SuccessMessage"] = dto.Id.HasValue ? "Car updated." : "Car added.";

            return RedirectToAction(nameof(Index), new { city = dto.City });
        }

        // ACTIVE / INACTIVE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, string? city)
        {
            var ok = await _carRentalService.ToggleCarAsync(id);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Car updated." : "Car was not found.";

            return RedirectToAction(nameof(Index), new { city });
        }

        // RENTAL: COMPLETE / CANCEL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RentalStatus(int id, CarRentalStatus status)
        {
            var ok = await _carRentalService.SetRentalStatusAsync(id, status);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Rental updated." : "This rental cannot be changed.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<string>> GetHotelCitiesAsync()
        {
            var hotels = await _hotelService.GetAllAsync();

            return hotels
                .Select(h => h.City)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }
}
