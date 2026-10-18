using System.Globalization;
using System.Text;

namespace HotelTravel.Web.Services.Api;

// Query string-i təhlükəsiz qurmaq üçün kiçik köməkçi:
// ApiUrl.For("api/cars/search").Add("city", "Baku").Add("pickupDate", date)
public class ApiUrl
{
    private readonly string _path;
    private readonly List<string> _parts = new();

    private ApiUrl(string path)
    {
        _path = path;
    }

    public static ApiUrl For(string path) => new(path);

    // URL içində dəyişən hissə (məs. təsdiq kodu)
    public static string Segment(string? value)
        => Uri.EscapeDataString(value?.Trim() ?? string.Empty);

    public ApiUrl Add(string name, object? value)
    {
        var text = Format(value);

        if (text != null)
            _parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(text)}");

        return this;
    }

    public override string ToString()
    {
        if (_parts.Count == 0)
            return _path;

        var builder = new StringBuilder(_path);
        builder.Append(_path.Contains('?') ? '&' : '?');
        builder.Append(string.Join("&", _parts));

        return builder.ToString();
    }

    public static implicit operator string(ApiUrl url) => url.ToString();

    private static string? Format(object? value) => value switch
    {
        null => null,
        string s when string.IsNullOrWhiteSpace(s) => null,
        string s => s.Trim(),
        DateTime d => d.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        Enum e => e.ToString(),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };
}
