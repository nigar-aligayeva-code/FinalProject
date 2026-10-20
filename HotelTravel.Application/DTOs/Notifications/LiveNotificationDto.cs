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
