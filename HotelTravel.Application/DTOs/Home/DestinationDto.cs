namespace HotelTravel.Application.DTOs.Home;

// Ana səhifədəki "Popular destinations" kartları
public class DestinationDto
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int HotelCount { get; set; }
    public int RoomCount { get; set; }

    // Bu şəhərdə ən ucuz gecəlik qiymət
    public decimal? MinPrice { get; set; }
}
