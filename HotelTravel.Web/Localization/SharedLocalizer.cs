using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Localization;

namespace HotelTravel.Web.Localization;

/// <summary>
/// Tərcümə: resx faylı eyni sözün böyük/kiçik hərfli iki variantını saxlaya bilmir
/// ("Guest" və "guest" - biri atılır, ekranda ingiliscə qalırdı). Ona görə:
///   1) kiçik hərflə başlayan söz (məs. T.Plural-dakı "adults") əvvəl "lc:adults" açarı ilə axtarılır;
///   2) sonra adi açar;
///   3) tapılmasa - hərf fərqi nəzərə alınmadan axtarılır və hərflər istənilən formaya salınır
///      ("View hotel" -> "Oteli gör"  /  "ROOMS" -> "OTAQLAR").
/// </summary>
public class SharedLocalizer : IStringLocalizer<SharedResource>
{
    public const string LowercasePrefix = "lc:";

    private static readonly ResourceManager IgnoreCaseResources = new(
        "HotelTravel.Web.Resources.SharedResource",
        typeof(SharedResource).Assembly)
    {
        IgnoreCase = true
    };

    private readonly IStringLocalizer _inner;

    public SharedLocalizer(IStringLocalizerFactory factory)
    {
        _inner = factory.Create(typeof(SharedResource));
    }

    public LocalizedString this[string name]
    {
        get
        {
            if (string.IsNullOrEmpty(name))
                return _inner[name ?? string.Empty];

            // İngiliscə: açarın özü göstərilir
            if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en")
                return _inner[name];

            if (char.IsLower(name[0]))
            {
                var lower = _inner[LowercasePrefix + name];

                if (!lower.ResourceNotFound)
                    return new LocalizedString(name, lower.Value, false);
            }

            var exact = _inner[name];

            if (!exact.ResourceNotFound)
                return exact;

            var other = FindIgnoringCase(name);

            return other == null
                ? exact
                : new LocalizedString(name, MatchCase(name, other), false);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var template = this[name];

            try
            {
                return new LocalizedString(
                    name,
                    string.Format(CultureInfo.CurrentCulture, template.Value, arguments),
                    template.ResourceNotFound);
            }
            catch (FormatException)
            {
                return template;
            }
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _inner.GetAllStrings(includeParentCultures);

    private static string? FindIgnoringCase(string name)
    {
        try
        {
            return IgnoreCaseResources.GetString(name, CultureInfo.CurrentUICulture);
        }
        catch (MissingManifestResourceException)
        {
            return null;
        }
        catch (MissingSatelliteAssemblyException)
        {
            return null;
        }
    }

    // Tərcümənin hərflərini istənilən açarın formasına sal
    public static string MatchCase(string key, string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var culture = CultureInfo.CurrentUICulture;
        var letters = key.Where(char.IsLetter).ToList();

        // "ROOMS" -> "OTAQLAR"
        if (letters.Count > 1 && letters.All(char.IsUpper))
            return value.ToUpper(culture);

        // "guest" -> "qonaq"
        if (char.IsLower(key[0]) && char.IsUpper(value[0]))
            return char.ToLower(value[0], culture) + value[1..];

        // "Guest" -> "Qonaq"
        if (char.IsUpper(key[0]) && char.IsLower(value[0]))
            return char.ToUpper(value[0], culture) + value[1..];

        return value;
    }
}
