using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Home;

// Ana səhifədəki "Guests' favourite rooms" kartları
public class FeaturedRoomDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int Capacity { get; set; }

    public decimal PricePerNight { get; set; }
    public string? MainImage { get; set; }

    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
