using Microsoft.Extensions.Localization;

namespace HotelTravel.Web.Localization;

/// <summary>
/// View-larda tərcümə köməkçiləri.
/// </summary>
public static class LocalizerExtensions
{
    /// <summary>
    /// Say + düzgün söz forması: T.Plural(3, "night", "nights") → "3 nights" / "3 gecə" / "3 ночей".
    /// </summary>
    public static string Plural(this IStringLocalizer localizer, int count, string one, string many) =>
        $"{count} {localizer[count == 1 ? one : many]}";

    /// <summary>
    /// Enum (və ya status mətni) tərcümə edir: T.Enum(BookingStatus.Cancelled) → "Ləğv edilib".
    /// "CheckedIn" kimi adlar açar kimi "Checked In" yazılır.
    /// </summary>
    public static string Enum(this IStringLocalizer localizer, object? value)
    {
        var name = value?.ToString();

        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var spaced = System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");

        return localizer[spaced];
    }
}
