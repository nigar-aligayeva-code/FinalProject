using HotelTravel.Application.DTOs.CityGuide;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CityGuideController : Controller
    {
        private readonly ICityGuideService _cityGuideService;
        private readonly IHotelService _hotelService;

        public CityGuideController(
            ICityGuideService cityGuideService,
            IHotelService hotelService)
        {
            _cityGuideService = cityGuideService;
            _hotelService = hotelService;
        }

        // LIST
        [HttpGet]
        public async Task<IActionResult> Index(string? city)
        {
            ViewBag.City = city;
            ViewBag.Cities = await GetHotelCitiesAsync();

            var places = await _cityGuideService.GetAllAsync(city);

            return View(places);
        }

        // CREATE / EDIT FORM
        [HttpGet]
        public async Task<IActionResult> Edit(int? id, string? city)
        {
            SaveCityPlaceDto? dto;

            if (id.HasValue)
            {
                dto = await _cityGuideService.GetForEditAsync(id.Value);

                if (dto is null)
                    return NotFound();
            }
            else
            {
                dto = new SaveCityPlaceDto { City = city ?? string.Empty };
            }

            ViewBag.Cities = await GetHotelCitiesAsync();

            return View(dto);
        }

        // SAVE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SaveCityPlaceDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Cities = await GetHotelCitiesAsync();
                return View(dto);
            }

            await _cityGuideService.SaveAsync(dto);

            TempData["SuccessMessage"] =
                dto.Id.HasValue ? "Place updated." : "Place added.";

            return RedirectToAction(nameof(Index), new { city = dto.City });
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? city)
        {
            var ok = await _cityGuideService.DeleteAsync(id);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Place deleted." : "Place was not found.";

            return RedirectToAction(nameof(Index), new { city });
        }

        // Otellərimizin olduğu şəhərlər (siyahı üçün)
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
