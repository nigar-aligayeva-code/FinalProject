namespace HotelTravel.Application.Services;

public record AvatarOption(string Key, string Label);

// Hazır avatarlar (şəkillər: wwwroot/assets/images/avatars/{key}.svg)
public static class AvatarCatalog
{
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
        new AvatarOption("silver-hair", "Silver hair")
    };

    // null / boş -> baş hərflər (etibarlıdır)
    public static bool IsValid(string? key)
        => string.IsNullOrWhiteSpace(key) ||
           All.Any(a => a.Key == key);

    // Etibarsız və ya boş açar -> null
    public static string? Normalize(string? key)
        => string.IsNullOrWhiteSpace(key) || !IsValid(key)
            ? null
            : key.Trim();

    public static string? GetUrl(string? key)
        => Normalize(key) is { } k
            ? $"/assets/images/avatars/{k}.svg"
            : null;

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
