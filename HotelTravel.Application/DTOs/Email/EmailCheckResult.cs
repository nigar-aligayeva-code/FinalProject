namespace HotelTravel.Application.DTOs.Email;

/// <summary>
/// E-poçt ünvanının yoxlanması: düzgün yazılıbmı, birdəfəlik (bot/fake) domen deyilmi,
/// domenin poçt serveri (MX) varmı. Suggestion: "gmial.com" -> "gmail.com".
/// </summary>
public class EmailCheckResult
{
    public bool IsValid { get; set; }

    public string? Reason { get; set; }

    // Məs. "nigar@gmail.com" (yazı səhvi varsa)
    public string? Suggestion { get; set; }

    // true: MX tapıldı, false: domenin poçt serveri yoxdur, null: yoxlamaq mümkün olmadı (internet yoxdur)
    public bool? HasMailServer { get; set; }

    public static EmailCheckResult Invalid(string reason, string? suggestion = null) =>
        new() { IsValid = false, Reason = reason, Suggestion = suggestion };
}
