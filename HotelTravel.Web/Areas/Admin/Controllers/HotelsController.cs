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
        private readonly IRoomService _roomService;

        public HotelsController(IHotelService hotelService, IRoomService roomService)
        {
            _hotelService = hotelService;
            _roomService = roomService;
        }

        public async Task<IActionResult> Index()
        {
            var hotels = await _hotelService.GetAllAsync();

            // Otellər və otaqlar bir yerdə: hər otelin altında öz otaqları
            ViewBag.RoomsByHotel = (await _roomService.GetAllAsync())
                .GroupBy(r => r.HotelId)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.RoomNumber).ToList());

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

            await LoadHotelRoomsAsync(id);

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateHotelDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadHotelRoomsAsync(id);
                return View(dto);
            }

            var updated = await _hotelService.UpdateAsync(id, dto);

            if (!updated)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Hotel updated.";

            return RedirectToAction(nameof(Edit), new { id });
        }

        // Otel redaktə səhifəsində bu otelin otaqları göstərilir
        private async Task LoadHotelRoomsAsync(int hotelId)
        {
            ViewBag.HotelId = hotelId;
            ViewBag.HotelRooms = (await _roomService.GetAllAsync())
                .Where(r => r.HotelId == hotelId)
                .OrderBy(r => r.RoomNumber)
                .ToList();
        }
    }
}