namespace HotelTravel.Domain.Entities;

public class Review : BaseEntity
{
    // 1 - 5 ulduz
    public int Rating { get; set; }

    public string Comment { get; set; }

    // Hər rezervasiyaya yalnız bir rəy
    public int BookingId { get; set; }
    public Booking Booking { get; set; }

    // Otaq üzrə rəyləri tez tapmaq üçün
    public int RoomId { get; set; }
    public Room Room { get; set; }

    // Otelin özünə verilən qiymət (xidmət, yer, təmizlik) — istəyə bağlıdır.
    // Otel Room.HotelId ilə tapılır, ayrıca sütun lazım deyil.
    public int? HotelRating { get; set; }

    public string? HotelComment { get; set; }
}
