using HotelTravel.Application.Interfaces;
using HotelTravel.Application.DTOs.Hotels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HotelsController : Controller
    {
        private readonly IHotelService _hotelService;

        public HotelsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        public async Task<IActionResult> Index()
        {
            var hotels = await _hotelService.GetAllAsync();

            return View(hotels);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var hotel = await _hotelService.GetByIdAsync(id);

            if (hotel == null)
            {
                return NotFound();
            }

            var dto = new UpdateHotelDto
            {
                Name = hotel.Name,
                Description = hotel.Description,
                Address = hotel.Address,
                City = hotel.City,
                Country = hotel.Country,
                Phone = hotel.Phone,
                Email = hotel.Email,
                MainImage = hotel.MainImage,
                Latitude = hotel.Latitude,
                Longitude = hotel.Longitude,
                StarRating = hotel.StarRating,
                HotelChainId = hotel.HotelChainId
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateHotelDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var updated = await _hotelService.UpdateAsync(id, dto);

            if (!updated)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}