namespace HotelTravel.Application.Options;

// appsettings.json -> "Email" bölməsi (hamısı istəyə bağlıdır)
public class EmailOptions
{
    public string From { get; set; } = "no-reply@tripio.az";
    public string FromName { get; set; } = "Tripio";

    // Məktubdakı və QR koddakı linklər üçün saytın ünvanı (MVC)
    public string PublicSiteUrl { get; set; } = "http://localhost:5233";

    // SMTP verilməyibsə məktublar .eml faylı kimi bu qovluğa yazılır
    public string? PickupDirectory { get; set; }

    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public bool EnableSsl { get; set; } = true;

    public bool UsesSmtp => !string.IsNullOrWhiteSpace(SmtpHost);
}
