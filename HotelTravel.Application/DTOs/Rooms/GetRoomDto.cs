using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Rooms;

public class GetRoomDto
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

    public List<string> AmenityNames { get; set; } = new();


    // =========================
    // GALLERY IMAGE URLS
    // =========================
    // Bunu hələ saxlayırıq, çünki Details və
    // digər mövcud View-lar bundan istifadə edir.
    public List<string> ImageUrls { get; set; } = new();


    // =========================
    // GALLERY IMAGES WITH ID
    // =========================
    // Admin Edit səhifəsində şəkli Id ilə
    // silmək üçün istifadə edəcəyik.
    public List<GetRoomImageDto> Images { get; set; } = new();
}