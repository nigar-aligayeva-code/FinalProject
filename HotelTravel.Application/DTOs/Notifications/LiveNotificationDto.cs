namespace HotelTravel.Application.DTOs.Notifications;

// Admin panelinə canlı bildiriş (SignalR)
public class LiveNotificationDto
{
    // "hotel", "flight", "taxi", "car", "trip", "cancel"
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public decimal? Amount { get; set; }
    public string? Code { get; set; }

    // Admin paneldə açılacaq səhifə (məs. "/Admin/Bookings")
    public string? Url { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

// Saytdakı bütün qonaqlara anonim fəaliyyət ("Someone just booked ...")
public class PublicActivityDto
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? City { get; set; }
}

// Qonağın özünə canlı bildiriş (yalnız daxil olmuş istifadəçiyə gedir)
public class GuestNotificationDto
{
    // Bazadakı bildirişin id-si (oxundu kimi işarələmək üçün)
    public int Id { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // "confirmed", "rejected", "cancelled", "hotel_reminder", "hotel_tomorrow", "flight_reminder", "flight_soon"
    public string Kind { get; set; } = string.Empty;

    // Otelin adı və ya uçuşun gedəcəyi şəhər
    public string Place { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    // Hazır formatda vaxt: "30 Oct" və ya "14:35"
    public string? When { get; set; }

    // Kliklədikdə açılacaq səhifə (sayt daxilində)
    public string? Url { get; set; }

    // İngiliscə mətn (brauzerdə tərcümə şablonu yoxdursa)
    public string Message { get; set; } = string.Empty;
}
