namespace HotelTravel.Application.DTOs.Rooms;

/// <summary>
/// Otaq müqayisəsi: seçilmiş otaqlar və hər meyar üzrə "ən yaxşı" otaq.
/// </summary>
public class RoomComparisonDto
{
    public List<GetRoomDto> Rooms { get; set; } = new();

    /// <summary>Bütün otaqlardakı imkanların birləşmiş siyahısı (əlifba sırası ilə).</summary>
    public List<string> AllAmenities { get; set; } = new();

    public int? CheapestRoomId { get; set; }
    public int? BestRatedRoomId { get; set; }
    public int? LargestRoomId { get; set; }
    public int? MostAmenitiesRoomId { get; set; }

    public bool HasAmenity(GetRoomDto room, string amenity) =>
        room.AmenityNames.Any(a => string.Equals(a, amenity, StringComparison.OrdinalIgnoreCase));
}
