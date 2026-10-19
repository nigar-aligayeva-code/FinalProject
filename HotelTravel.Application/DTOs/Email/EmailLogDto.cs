namespace HotelTravel.Application.DTOs.Email;

public class EmailLogDto
{
    public int Id { get; set; }
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ReferenceCode { get; set; }
    public bool HasAttachment { get; set; }
    public bool Delivered { get; set; }
    public string? Error { get; set; }
    public DateTime SentAt { get; set; }

    // Siyahıda boş gəlir, yalnız tək məktub açılanda doldurulur
    public string? HtmlBody { get; set; }
}
