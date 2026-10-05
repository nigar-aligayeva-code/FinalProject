using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelTravel.Web.Controllers;

public class RoomsController : Controller
{
    private readonly IRoomService _roomService;
    private readonly IHotelService _hotelService;
    private readonly IAmenityService _amenityService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public RoomsController(
        IRoomService roomService,
        IHotelService hotelService,
        IAmenityService amenityService,
        IWebHostEnvironment webHostEnvironment)
    {
        _roomService = roomService;
        _hotelService = hotelService;
        _amenityService = amenityService;
        _webHostEnvironment = webHostEnvironment;
    }


    // =========================
    // ADMIN - ROOM LIST
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rooms =
            await _roomService.GetAllAsync();

        return View(rooms);
    }


    // =========================
    // PUBLIC - ROOM DETAILS
    // =========================
    [HttpGet]
    public async Task<IActionResult> Details(
     int id,
     DateTime? checkInDate,
     DateTime? checkOutDate,
     int? adultCount,
     int? childrenCount)
    {
        var room =
            await _roomService.GetByIdAsync(id);

        if (room is null)
            return NotFound();

        // Search-dən gələn məlumatları
        // Details səhifəsinə ötürürük
        ViewBag.CheckInDate = checkInDate;
        ViewBag.CheckOutDate = checkOutDate;
        ViewBag.AdultCount = adultCount;
        ViewBag.ChildrenCount = childrenCount;

        return View(room);
    }


    // =========================
    // ADMIN - CREATE ROOM
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadHotelsAsync();
        await LoadAmenitiesAsync();

        return View();
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateRoomDto dto,
        IFormFile? mainImage,
        List<IFormFile>? galleryImages)
    {
        // MainImage artıq string input kimi gəlmir.
        // Ona görə onun validation error-unu silirik.
        ModelState.Remove(nameof(dto.MainImage));

        if (!ModelState.IsValid)
        {
            await LoadHotelsAsync(dto.HotelId);
            await LoadAmenitiesAsync();

            return View(dto);
        }

        // =========================
        // MAIN IMAGE
        // =========================
        if (mainImage != null &&
            mainImage.Length > 0)
        {
            var mainImageUrl =
                await SaveSingleRoomImageAsync(
                    mainImage);

            if (!string.IsNullOrWhiteSpace(mainImageUrl))
            {
                dto.MainImage =
                    mainImageUrl;
            }
        }

        // =========================
        // GALLERY IMAGES
        // =========================
        dto.ImageUrls =
            await SaveRoomImagesAsync(
                galleryImages);

        await _roomService.CreateAsync(dto);

        return RedirectToAction(nameof(Index));
    }


    // =========================
    // ADMIN - EDIT ROOM
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var room =
            await _roomService.GetByIdAsync(id);

        if (room is null)
            return NotFound();

        var dto = new UpdateRoomDto
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            Name = room.Name,
            Description = room.Description,
            PricePerNight = room.PricePerNight,
            Capacity = room.Capacity,
            MainImage = room.MainImage,
            RoomType = room.RoomType,
            HotelId = room.HotelId,

            AmenityIds = room.AmenityIds,

            ImageUrls = room.ImageUrls,

            Images = room.Images
        };

        await LoadHotelsAsync(dto.HotelId);
        await LoadAmenitiesAsync();

        return View(dto);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        UpdateRoomDto dto,
        IFormFile? mainImage,
        List<IFormFile>? galleryImages)
    {
        // Mövcud room-u tapırıq.
        var currentRoom =
            await _roomService.GetByIdAsync(id);

        if (currentRoom is null)
            return NotFound();


        // =========================
        // KEEP CURRENT MAIN IMAGE
        // =========================

        // MainImage artıq View-dan string input
        // kimi göndərilmir.
        // Ona görə əvvəlcə köhnə şəkli saxlayırıq.
        dto.MainImage =
            currentRoom.MainImage;

        // Model binding zamanı MainImage üçün
        // yaranmış validation error-u silirik.
        ModelState.Remove(nameof(dto.MainImage));


        // =========================
        // VALIDATION
        // =========================
        if (!ModelState.IsValid)
        {
            dto.ImageUrls =
                currentRoom.ImageUrls;

            dto.Images =
                currentRoom.Images;

            await LoadHotelsAsync(dto.HotelId);
            await LoadAmenitiesAsync();

            return View(dto);
        }

        // =========================
        // CHANGE MAIN IMAGE
        // =========================
        if (mainImage != null &&
            mainImage.Length > 0)
        {
            var newMainImage =
                await SaveSingleRoomImageAsync(
                    mainImage);

            if (!string.IsNullOrWhiteSpace(
                    newMainImage))
            {
                dto.MainImage =
                    newMainImage;
            }
        }


        // =========================
        // ADD NEW GALLERY IMAGES
        // =========================
        var newImageUrls =
            await SaveRoomImagesAsync(
                galleryImages);

        dto.ImageUrls =
            newImageUrls;
        // =========================
        // DELETE MAIN IMAGE ON SAVE
        // =========================
        if (dto.RemoveMainImage &&
            !string.IsNullOrWhiteSpace(currentRoom.MainImage))
        {
            var oldMainImage =
                currentRoom.MainImage;

            // Eyni fayl gallery-də istifadə olunursa,
            // fiziki faylı silmirik.
            var isUsedInGallery =
                currentRoom.ImageUrls != null &&
                currentRoom.ImageUrls.Any(x =>
                    string.Equals(
                        x,
                        oldMainImage,
                        StringComparison.OrdinalIgnoreCase));

            var deleted =
                await _roomService.DeleteMainImageAsync(id);

            if (deleted &&
                !isUsedInGallery &&
                oldMainImage.StartsWith(
                    "/uploads/rooms/",
                    StringComparison.OrdinalIgnoreCase))
            {
                var fileName =
                    Path.GetFileName(oldMainImage);

                var filePath =
                    Path.Combine(
                        _webHostEnvironment.WebRootPath,
                        "uploads",
                        "rooms",
                        fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // UpdateAsync köhnə MainImage-i yenidən DB-yə
            // yazmasın deyə DTO-nu da boşaldırıq.
            dto.MainImage = string.Empty;
        }
        // =========================
        // DELETE SELECTED GALLERY IMAGES
        // =========================
        if (dto.DeletedImageIds != null &&
            dto.DeletedImageIds.Any())
        {
            foreach (var imageId in dto.DeletedImageIds)
            {
                // Şəklin məlumatını silməzdən əvvəl götürürük
                var image =
                    await _roomService.GetImageByIdAsync(
                        imageId);

                if (image is null)
                    continue;

                // Database-də soft delete
                var deleted =
                    await _roomService.DeleteImageAsync(
                        imageId);

                if (!deleted)
                    continue;

                // Fiziki faylı sil
                if (!string.IsNullOrWhiteSpace(image.ImageUrl) &&
                    image.ImageUrl.StartsWith(
                        "/uploads/rooms/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var fileName =
                        Path.GetFileName(
                            image.ImageUrl);

                    var filePath =
                        Path.Combine(
                            _webHostEnvironment.WebRootPath,
                            "uploads",
                            "rooms",
                            fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
        }
        // =========================
        // UPDATE ROOM
        // =========================
        var result =
            await _roomService.UpdateAsync(
                id,
                dto);

        if (!result)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }


    // =========================
    // ADMIN - DELETE ROOM
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var room =
            await _roomService.GetByIdAsync(id);

        if (room is null)
            return NotFound();

        return View(room);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var result =
            await _roomService.DeleteAsync(id);

        if (!result)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }
    // =========================
    // ADMIN - DELETE GALLERY IMAGE
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGalleryImage(
        int roomId,
        int imageId)
    {
        // =========================
        // FIND IMAGE
        // =========================
        var image =
            await _roomService.GetImageByIdAsync(
                imageId);

        if (image is null)
            return NotFound();


        // =========================
        // SOFT DELETE FROM DATABASE
        // =========================
        var result =
            await _roomService.DeleteImageAsync(
                imageId);

        if (!result)
            return NotFound();


        // =========================
        // DELETE PHYSICAL FILE
        // =========================
        if (!string.IsNullOrWhiteSpace(image.ImageUrl) &&
            image.ImageUrl.StartsWith(
                "/uploads/rooms/",
                StringComparison.OrdinalIgnoreCase))
        {
            var fileName =
                Path.GetFileName(
                    image.ImageUrl);

            var filePath =
                Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "rooms",
                    fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }


        // =========================
        // BACK TO EDIT PAGE
        // =========================
        return RedirectToAction(
            nameof(Edit),
            new { id = roomId });
    }


    // =========================
    // ADMIN - DELETE MAIN IMAGE
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMainImage(
        int roomId)
    {
        // =========================
        // FIND ROOM
        // =========================
        var room =
            await _roomService.GetByIdAsync(
                roomId);

        if (room is null)
            return NotFound();


        // =========================
        // KEEP OLD MAIN IMAGE URL
        // =========================
        var mainImageUrl =
            room.MainImage;


        // =========================
        // CHECK IF SAME IMAGE IS
        // USED IN GALLERY
        // =========================
        var isUsedInGallery =
            room.ImageUrls != null &&
            room.ImageUrls.Any(x =>
                string.Equals(
                    x,
                    mainImageUrl,
                    StringComparison.OrdinalIgnoreCase));


        // =========================
        // DELETE MAIN IMAGE FROM DB
        // =========================
        var result =
            await _roomService.DeleteMainImageAsync(
                roomId);

        if (!result)
            return NotFound();


        // =========================
        // DELETE PHYSICAL FILE
        // =========================
        // Əgər eyni file gallery-də də istifadə olunursa,
        // fiziki faylı silmirik.
        if (!isUsedInGallery &&
            !string.IsNullOrWhiteSpace(mainImageUrl) &&
            mainImageUrl.StartsWith(
                "/uploads/rooms/",
                StringComparison.OrdinalIgnoreCase))
        {
            var fileName =
                Path.GetFileName(
                    mainImageUrl);

            var filePath =
                Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "rooms",
                    fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }


        // =========================
        // BACK TO EDIT PAGE
        // =========================
        return RedirectToAction(
            nameof(Edit),
            new { id = roomId });
    }


    
    // =========================
    // PUBLIC - SEARCH ROOMS
    // =========================
    [HttpGet]
    public async Task<IActionResult> Search()
    {
        await LoadFullyBookedDatesAsync();

        return View(new RoomSearchDto());
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(
     RoomSearchDto search)
    {
        // =========================
        // REQUIRED VALIDATION
        // =========================
        if (!ModelState.IsValid)
        {
            await LoadFullyBookedDatesAsync();

            return View(search);
        }


        // Buradan sonra bilirik ki,
        // hər iki tarix mütləq seçilib.
        var checkInDate =
            search.CheckInDate!.Value;

        var checkOutDate =
            search.CheckOutDate!.Value;


        // =========================
        // CHECK-IN CANNOT BE PAST
        // =========================
        if (checkInDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(search.CheckInDate),
                "Check-in date cannot be in the past.");

            await LoadFullyBookedDatesAsync();

            return View(search);
        }


        // =========================
        // CHECK-OUT AFTER CHECK-IN
        // =========================
        if (checkInDate.Date >= checkOutDate.Date)
        {
            ModelState.AddModelError(
                nameof(search.CheckOutDate),
                "Check-out date must be after check-in date.");

            await LoadFullyBookedDatesAsync();

            return View(search);
        }


        // =========================
        // SEARCH AVAILABLE ROOMS
        // =========================
        var rooms =
            await _roomService.SearchAvailableRoomsAsync(
                search);


        // =========================
        // SEND SEARCH INFO TO VIEW
        // =========================
        ViewBag.CheckInDate =
            checkInDate;

        ViewBag.CheckOutDate =
            checkOutDate;

        ViewBag.AdultCount =
            search.AdultCount;

        ViewBag.ChildrenCount =
            search.ChildrenCount;


        return View(
            "AvailableRooms",
            rooms);
    }
    // =========================
    // SAVE SINGLE ROOM IMAGE
    // =========================
    private async Task<string?>
        SaveSingleRoomImageAsync(
            IFormFile? image)
    {
        if (image == null ||
            image.Length == 0)
        {
            return null;
        }


        // =========================
        // ALLOWED EXTENSIONS
        // =========================
        var allowedExtensions =
            new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };


        var extension =
            Path.GetExtension(
                    image.FileName)
                .ToLowerInvariant();


        if (!allowedExtensions.Contains(extension))
            return null;


        // =========================
        // MAXIMUM 5 MB
        // =========================
        if (image.Length >
            5 * 1024 * 1024)
        {
            return null;
        }


        // =========================
        // UPLOAD FOLDER
        // =========================
        var uploadFolder =
            Path.Combine(
                _webHostEnvironment.WebRootPath,
                "uploads",
                "rooms");


        Directory.CreateDirectory(
            uploadFolder);


        // =========================
        // UNIQUE FILE NAME
        // =========================
        var fileName =
            $"{Guid.NewGuid():N}{extension}";


        var filePath =
            Path.Combine(
                uploadFolder,
                fileName);


        // =========================
        // SAVE FILE
        // =========================
        await using var stream =
            new FileStream(
                filePath,
                FileMode.Create);


        await image.CopyToAsync(stream);


        // Path saved to database
        return
            $"/uploads/rooms/{fileName}";
    }


    // =========================
    // SAVE GALLERY IMAGES
    // =========================
    private async Task<List<string>>
        SaveRoomImagesAsync(
            List<IFormFile>? images)
    {
        var imageUrls =
            new List<string>();


        if (images == null ||
            images.Count == 0)
        {
            return imageUrls;
        }


        foreach (var image in images)
        {
            var imageUrl =
                await SaveSingleRoomImageAsync(
                    image);


            if (!string.IsNullOrWhiteSpace(
                    imageUrl))
            {
                imageUrls.Add(
                    imageUrl);
            }
        }


        return imageUrls;
    }


    // =========================
    // LOAD HOTELS
    // =========================
    private async Task LoadHotelsAsync(
        int? selectedHotelId = null)
    {
        var hotels =
            await _hotelService.GetAllAsync();


        ViewBag.Hotels =
            new SelectList(
                hotels,
                "Id",
                "Name",
                selectedHotelId);
    }


    // =========================
    // LOAD AMENITIES
    // =========================
    private async Task LoadAmenitiesAsync()
    {
        ViewBag.Amenities =
            await _amenityService.GetAllAsync();
    }


    // =========================
    // LOAD FULLY BOOKED DATES
    // =========================
    private async Task
        LoadFullyBookedDatesAsync()
    {
        var startDate =
            DateTime.Today;

        var endDate =
            DateTime.Today.AddYears(1);


        var fullyBookedDates =
            await _roomService
                .GetFullyBookedDatesAsync(
                    startDate,
                    endDate);


        ViewBag.FullyBookedDates =
            fullyBookedDates
                .Select(d =>
                    d.ToString("yyyy-MM-dd"))
                .ToList();
    }
}