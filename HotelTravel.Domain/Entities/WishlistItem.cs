namespace HotelTravel.Domain.Entities;

// İstifadəçinin "Saved" (favori) otaqları
public class WishlistItem : BaseEntity
{
    public string AppUserId { get; set; } = string.Empty;
    public AppUser AppUser { get; set; } = null!;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
}
