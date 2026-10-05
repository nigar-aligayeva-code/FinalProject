using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Rooms;

public class UpdateRoomDto
{
    public int Id { get; set; }
    public string RoomNumber { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }

    public string MainImage { get; set; }

    public RoomType RoomType { get; set; }

    public int HotelId { get; set; }


    // =========================
    // AMENITIES
    // =========================
    public List<int> AmenityIds { get; set; } = new();


    // =========================
    // NEW GALLERY IMAGE URLS
    // =========================
    // Edit zamanı yeni upload olunan şəkillər
    public List<string> ImageUrls { get; set; } = new();


    // =========================
    // EXISTING GALLERY IMAGES
    // =========================
    // Mövcud şəkillərin Id + ImageUrl məlumatı
    // Delete düyməsi üçün istifadə olunacaq
    public List<GetRoomImageDto> Images { get; set; } = new();
    // Main image Save Changes zamanı silinsin?
    public bool RemoveMainImage { get; set; }

    // Gallery-dən Save Changes zamanı silinəcək image Id-ləri
    public List<int> DeletedImageIds { get; set; } = new();
}