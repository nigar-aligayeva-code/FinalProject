namespace HotelTravel.Domain.Entities;

// Göndərilən hər məktubun qeydi (admin "Emails" səhifəsində görünür)
public class EmailLog : BaseEntity
{
    public string To { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;

    // Məs. "BookingConfirmation", "BookingCancellation"
    public string Type { get; set; } = string.Empty;

    // Rezervasiyanın təsdiq kodu
    public string? ReferenceCode { get; set; }

    public bool HasAttachment { get; set; }

    public bool Delivered { get; set; }

    public string? Error { get; set; }
}
