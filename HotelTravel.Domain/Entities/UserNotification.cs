namespace HotelTravel.Domain.Entities;

// Qonağın bildirişi (saytın yuxarısındakı 🔔): təsdiq, rədd, ləğv, xatırlatma.
// Bazada saxlanılır ki, qonaq saytda olmayanda gələnləri sonra görsün.
public class UserNotification : BaseEntity
{
    public string AppUserId { get; set; } = string.Empty;

    // "confirmed", "rejected", "cancelled", "hotel_reminder", ...
    public string Kind { get; set; } = string.Empty;

    // Otelin adı və ya uçuşun şəhəri
    public string Place { get; set; } = string.Empty;

    // Rezervasiya kodu
    public string Code { get; set; } = string.Empty;

    // Hazır formatda tarix / saat ("30 Oct", "14:35")
    public string? When { get; set; }

    public string? Url { get; set; }

    // İngiliscə mətn (tərcümə şablonu olmayanda)
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }
}
