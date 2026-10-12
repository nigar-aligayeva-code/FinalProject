
using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoomsController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly IHotelService _hotelService;
        private readonly IAmenityService _amenityService;

        public RoomsController(
            IRoomService roomService,
            IHotelService hotelService,
            IAmenityService amenityService)
        {
            _roomService = roomService;
            _hotelService = hotelService;
            _amenityService = amenityService;
        }

        // =========================
        // ROOM LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            var rooms = await _roomService.GetAllAsync();
            var hotels = await _hotelService.GetAllAsync();

            ViewBag.HotelNames = hotels.ToDictionary(
                h => h.Id,
                h => h.Name
            );

            return View(rooms);
        }

        // =========================
        // EDIT ROOM - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _roomService.GetByIdAsync(id);

            if (room == null)
                return NotFound();

            await LoadDataAsync();

            var dto = new UpdateRoomDto
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                Name = room.Name,
                Description = room.Description,
                PricePerNight = room.PricePerNight,
                Capacity = room.Capacity,
                MainImage = room.MainImage,
                PanoramaImage = room.PanoramaImage,
                RoomType = room.RoomType,
                HotelId = room.HotelId,
                AmenityIds = room.AmenityIds.ToList(),
                Images = room.Images.ToList()
            };

            return View(dto);
        }

        // =========================
        // EDIT ROOM - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            UpdateRoomDto dto)
        {
            if (id != dto.Id)
                return BadRequest();

            var existing = await _roomService.GetByIdAsync(id);

            if (existing == null)
                return NotFound();

            // Keep current main image.
            dto.MainImage = existing.MainImage;

            // MainImage is not posted as a text field.
            ModelState.Remove(nameof(dto.MainImage));

            // Gallery display fields are not form inputs.
            ModelState.Remove(nameof(dto.Images));
            ModelState.Remove(nameof(dto.ImageUrls));

            if (!Enum.IsDefined(typeof(RoomType), dto.RoomType))
            {
                ModelState.AddModelError(
                    nameof(dto.RoomType),
                    "Invalid room type.");
            }

            if (dto.PricePerNight < 0)
            {
                ModelState.AddModelError(
                    nameof(dto.PricePerNight),
                    "Price cannot be negative.");
            }

            if (dto.Capacity < 1)
            {
                ModelState.AddModelError(
                    nameof(dto.Capacity),
                    "Capacity must be at least 1.");
            }

            // Preserve selected amenities.
            // Empty list means no amenities selected.
            dto.AmenityIds ??= new List<int>();

            // Do not add duplicate gallery images.
            dto.ImageUrls = new List<string>();

            // Existing gallery images are retained.
            dto.Images = existing.Images.ToList();

            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return View(dto);
            }

            var updated = await _roomService.UpdateAsync(id, dto);

            if (!updated)
                return NotFound();

            TempData["Success"] = "Room updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE ROOM
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _roomService.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            TempData["Success"] = "Room deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // LOAD HOTELS & AMENITIES
        // =========================
        private async Task LoadDataAsync()
        {
            var hotels = await _hotelService.GetAllAsync();
            var amenities = await _amenityService.GetAllAsync();

            ViewBag.Hotels = hotels
                .OrderBy(h => h.Name)
                .ToList();

            ViewBag.Amenities = amenities;
        }
    }
}
