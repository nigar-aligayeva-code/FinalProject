namespace HotelTravel.Application.Options;

// User Secrets (API layihəsi) -> "Twilio": { "AccountSid": "AC...", "AuthToken": "...", "FromNumber": "+1...", "TestPhoneOverride": "+99450..." }
// Pulsuz sınaq hesabı (trial) yalnız təsdiqlənmiş nömrələrə zəng edə bilir. https://www.twilio.com
public class TwilioOptions
{
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }

    // Twilio-nun verdiyi nömrə (zəng bu nömrədən gəlir)
    public string? FromNumber { get; set; }

    // Doluysa, zəng otelin əsl nömrəsinə yox, bu nömrəyə gedir (demo / müdafiə üçün öz telefonun)
    public string? TestPhoneOverride { get; set; }

    public string BaseUrl { get; set; } = "https://api.twilio.com/2010-04-01/";

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(AccountSid) &&
        !string.IsNullOrWhiteSpace(AuthToken) &&
        !string.IsNullOrWhiteSpace(FromNumber);
}
