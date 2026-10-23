namespace HotelTravel.Application.Services;

/// <summary>
/// Saytın dəstəklədiyi dillər. Pul və tarix formatı dəyişmir (həmişə en-US),
/// yalnız mətnlər tərcümə olunur ki, qiymət yazılışı (120.50) heç vaxt pozulmasın.
/// </summary>
public static class LanguageRules
{
    public const string Default = "en";

    public static readonly IReadOnlyList<(string Code, string Label, string Flag)> Languages = new[]
    {
        ("en", "English", "🇬🇧"),
        ("az", "Azərbaycanca", "🇦🇿"),
        ("ru", "Русский", "🇷🇺")
    };

    public static IEnumerable<string> Codes => Languages.Select(l => l.Code);

    /// <summary>"AZ", " ru ", "az-Latn-AZ" → "az"/"ru"; tanınmayan dil → "en".</summary>
    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Default;

        var shortCode = code.Trim().Split('-', '_')[0].ToLowerInvariant();

        return Codes.Contains(shortCode) ? shortCode : Default;
    }

    public static string LabelOf(string? code) =>
        Languages.First(l => l.Code == Normalize(code)).Label;
}
