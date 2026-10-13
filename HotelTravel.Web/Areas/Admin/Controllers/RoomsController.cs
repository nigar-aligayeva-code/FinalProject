
using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        private readonly IWebHostEnvironment _environment;

        private const long MaxImageSize = 5 * 1024 * 1024;

        public RoomsController(
            IRoomService roomService,
            IHotelService hotelService,
            IAmenityService amenityService,
            IWebHostEnvironment environment)
        {
            _roomService = roomService;
            _hotelService = hotelService;
            _amenityService = amenityService;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _roomService.GetAllAsync();
            var hotels = await _hotelService.GetAllAsync();

            ViewBag.HotelNames = hotels.ToDictionary(
                h => h.Id,
                h => h.Name);

            return View(rooms);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            UpdateRoomDto dto,
            IFormFile? mainImage,
            List<IFormFile>? galleryImages)
        {
            if (id != dto.Id)
                return BadRequest();

            var existing = await _roomService.GetByIdAsync(id);

            if (existing == null)
                return NotFound();

            dto.AmenityIds ??= new List<int>();
            dto.DeletedImageIds ??= new List<int>();

            // These fields are managed on the server.
            ModelState.Remove(nameof(dto.MainImage));
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

            if (mainImage != null && mainImage.Length > 0)
            {
                ValidateImage(mainImage, "mainImage");
            }

            var galleryFiles = galleryImages?
                .Where(f => f.Length > 0)
                .ToList() ?? new List<IFormFile>();

            foreach (var file in galleryFiles)
            {
                ValidateImage(file, "galleryImages");
            }

            // Only allow deletion of images belonging to this room.
            var existingImageIds = existing.Images
                .Select(i => i.Id)
                .ToHashSet();

            var requestedDeleteIds = dto.DeletedImageIds
                .Distinct()
                .ToList();

            if (requestedDeleteIds.Any(
                imageId => !existingImageIds.Contains(imageId)))
            {
                ModelState.AddModelError(
                    nameof(dto.DeletedImageIds),
                    "Invalid gallery image selection.");
            }

            if (!ModelState.IsValid)
            {
                dto.MainImage = existing.MainImage;
                dto.Images = existing.Images.ToList();

                await LoadDataAsync();

                return View(dto);
            }

            var savedFiles = new List<string>();

            try
            {
                // MAIN IMAGE
                dto.MainImage = existing.MainImage;

                if (dto.RemoveMainImage)
                {
                    dto.MainImage = string.Empty;
                }

                if (mainImage != null && mainImage.Length > 0)
                {
                    dto.MainImage = await SaveImageAsync(
                        mainImage,
                        savedFiles);
                }

                // GALLERY IMAGES
                dto.ImageUrls = new List<string>();

                foreach (var file in galleryFiles)
                {
                    var imageUrl = await SaveImageAsync(
                        file,
                        savedFiles);

                    dto.ImageUrls.Add(imageUrl);
                }

                dto.Images = existing.Images.ToList();

                var updated = await _roomService.UpdateAsync(
                    id,
                    dto);

                if (!updated)
                {
                    DeleteNewFiles(savedFiles);
                    return NotFound();
                }
            }
            catch
            {
                DeleteNewFiles(savedFiles);
                throw;
            }

            // The room update has succeeded.
            // Delete selected gallery images from the database.
            foreach (var imageId in requestedDeleteIds)
            {
                await _roomService.DeleteImageAsync(imageId);
            }

            TempData["Success"] =
                "Room and images updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _roomService.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            TempData["Success"] =
                "Room deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDataAsync()
        {
            var hotels = await _hotelService.GetAllAsync();
            var amenities = await _amenityService.GetAllAsync();

            ViewBag.Hotels = hotels
                .OrderBy(h => h.Name)
                .ToList();

            ViewBag.Amenities = amenities;
        }

        private void ValidateImage(
            IFormFile file,
            string fieldName)
        {
            var extension = Path.GetExtension(
                file.FileName).ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    fieldName,
                    "Only JPG, PNG and WEBP images are allowed.");
            }

            if (file.Length > MaxImageSize)
            {
                ModelState.AddModelError(
                    fieldName,
                    "Image size cannot exceed 5 MB.");
            }

            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedContentTypes.Contains(
                file.ContentType.ToLowerInvariant()))
            {
                ModelState.AddModelError(
                    fieldName,
                    "Invalid image content type.");
            }
        }

        private async Task<string> SaveImageAsync(
            IFormFile file,
            List<string> savedFiles)
        {
            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            var uploadFolder = Path.Combine(
                webRoot,
                "uploads",
                "rooms");

            Directory.CreateDirectory(uploadFolder);

            var extension = Path.GetExtension(
                file.FileName).ToLowerInvariant();

            var fileName = $"{Guid.NewGuid():N}{extension}";

            var fullPath = Path.Combine(
                uploadFolder,
                fileName);

            using (var stream = new FileStream(
                fullPath,
                FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            savedFiles.Add(fullPath);

            return $"/uploads/rooms/{fileName}";
        }

        private static void DeleteNewFiles(
            IEnumerable<string> filePaths)
        {
            foreach (var path in filePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
        }
    }
}
