namespace HotelTravel.Application.DTOs.Email;

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;

    // Jurnala (Admin → Emails) yazılacaq mətn. Şəxsi link/token olan məktublarda
    // gizlədilmiş versiya verilir; boşdursa HtmlBody yazılır.
    public string? LogHtmlBody { get; set; }

    public List<EmailAttachment> Attachments { get; set; } = new();
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
