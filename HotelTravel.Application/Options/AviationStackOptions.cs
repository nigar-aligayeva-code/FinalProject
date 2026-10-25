namespace HotelTravel.Application.Options;

// User Secrets -> "AviationStack": { "AccessKey": "..." }
// Pulsuz plan: ayda 100 sorğu, yalnız HTTP. https://aviationstack.com
public class AviationStackOptions
{
    public string? AccessKey { get; set; }

    public string BaseUrl { get; set; } = "http://api.aviationstack.com/v1/";

    public bool IsEnabled => !string.IsNullOrWhiteSpace(AccessKey);
}
