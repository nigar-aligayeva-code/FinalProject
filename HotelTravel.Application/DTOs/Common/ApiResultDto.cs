namespace HotelTravel.Application.DTOs.Common;

// API əməliyyatlarının ümumi cavabı (MVC HttpClient ilə oxuyur)
public class ApiResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    // Məs. rezervasiyanın təsdiq kodu
    public string? Code { get; set; }

    // Məs. yeni yaradılan / əlaqəli obyektin Id-si
    public int? Id { get; set; }

    // Əlavə bool nəticə (məs. wishlist-də "saved")
    public bool? Flag { get; set; }

    public static ApiResultDto Ok(
        string message = "OK",
        string? code = null,
        int? id = null,
        bool? flag = null)
        => new()
        {
            Success = true,
            Message = message,
            Code = code,
            Id = id,
            Flag = flag
        };

    public static ApiResultDto Fail(string message)
        => new()
        {
            Success = false,
            Message = message
        };
}
