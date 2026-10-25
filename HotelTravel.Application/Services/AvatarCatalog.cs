using System.Text.RegularExpressions;

namespace HotelTravel.Application.Services;

public record AvatarOption(string Key, string Label);

public record SkinTone(int Number, string Color, string Label);

// Avatarlar 3 növdür:
//   1) hazır stil:            "beard"            -> /assets/images/avatars/beard.svg
//   2) stil + dəri tonu:      "beard-t4"         -> /assets/images/avatars/beard-t4.svg  (t1 açıq ... t5 tünd)
//   3) istifadəçinin şəkli:   "/uploads/avatars/{32 hex}.jpg|png|webp"
// null / boş -> baş hərflər.
public static class AvatarCatalog
{
    public const string UploadFolder = "/uploads/avatars/";

    public static readonly IReadOnlyList<AvatarOption> All = new[]
    {
        new AvatarOption("short-hair", "Short hair"),
        new AvatarOption("long-hair", "Long hair"),
        new AvatarOption("beard", "Beard"),
        new AvatarOption("mustache", "Mustache"),
        new AvatarOption("headscarf", "Headscarf"),
        new AvatarOption("curly-hair", "Curly hair"),
        new AvatarOption("hair-bun", "Hair bun"),
        new AvatarOption("bald", "Bald"),
        new AvatarOption("glasses", "Glasses"),
        new AvatarOption("cap", "Cap"),
        new AvatarOption("bob-cut", "Bob cut"),
        new AvatarOption("silver-hair", "Silver hair"),
        new AvatarOption("afro", "Afro"),
        new AvatarOption("braids", "Braids"),
        new AvatarOption("dreadlocks", "Dreadlocks"),
        new AvatarOption("turban", "Turban")
    };

    public static readonly IReadOnlyList<SkinTone> Tones = new[]
    {
        new SkinTone(1, "#f6d7c3", "Light"),
        new SkinTone(2, "#e8b892", "Medium light"),
        new SkinTone(3, "#c68a5e", "Medium"),
        new SkinTone(4, "#9a6440", "Medium dark"),
        new SkinTone(5, "#6b4428", "Dark")
    };

    private static readonly Regex ToneKey = new(@"^(?<style>[a-z-]+)-t(?<tone>[1-5])$", RegexOptions.Compiled);

    // Yalnız bizim yaratdığımız fayl adı: təsadüfi 32 simvol + icazəli uzantı (path traversal mümkün deyil)
    private static readonly Regex UploadKey = new(@"^/uploads/avatars/[a-f0-9]{32}\.(jpg|png|webp)$", RegexOptions.Compiled);

    private static bool IsStyle(string key) => All.Any(a => a.Key == key);

    public static bool IsUpload(string? key) => key is not null && UploadKey.IsMatch(key);

    /// <summary>"beard" + 4 -> "beard-t4"; tone 0 -> stilin özü.</summary>
    public static string WithTone(string style, int tone) =>
        tone is >= 1 and <= 5 ? $"{style}-t{tone}" : style;

    /// <summary>Açardakı dəri tonu (yoxdursa 0).</summary>
    public static int ToneOf(string? key) =>
        key is not null && ToneKey.Match(key) is { Success: true } m ? int.Parse(m.Groups["tone"].Value) : 0;

    /// <summary>Açardakı stil ("beard-t4" -> "beard").</summary>
    public static string? StyleOf(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || IsUpload(key))
            return null;

        var m = ToneKey.Match(key);
        var style = m.Success ? m.Groups["style"].Value : key;

        return IsStyle(style) ? style : null;
    }

    // null / boş -> baş hərflər (etibarlıdır)
    public static bool IsValid(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return true;

        key = key.Trim();

        if (IsStyle(key) || IsUpload(key))
            return true;

        var m = ToneKey.Match(key);
        return m.Success && IsStyle(m.Groups["style"].Value);
    }

    // Etibarsız və ya boş açar -> null
    public static string? Normalize(string? key)
        => string.IsNullOrWhiteSpace(key) || !IsValid(key)
            ? null
            : key.Trim();

    public static string? GetUrl(string? key)
    {
        var k = Normalize(key);

        if (k is null)
            return null;

        return IsUpload(k) ? k : $"/assets/images/avatars/{k}.svg";
    }

    // Baş hərflər: "Nigar Aliyeva" -> "NA"; ad yoxdursa email-in ilk hərfi
    public static string GetInitials(string? firstName, string? lastName, string? fallback = null)
    {
        var first = string.IsNullOrWhiteSpace(firstName) ? "" : firstName.Trim()[..1];
        var last = string.IsNullOrWhiteSpace(lastName) ? "" : lastName.Trim()[..1];
        var initials = (first + last).ToUpperInvariant();

        if (initials.Length > 0)
            return initials;

        return string.IsNullOrWhiteSpace(fallback)
            ? "?"
            : fallback.Trim()[..1].ToUpperInvariant();
    }
}
