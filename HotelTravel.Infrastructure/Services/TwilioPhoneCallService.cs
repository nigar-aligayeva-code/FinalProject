using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Options;
using HotelTravel.Application.Services;
using Microsoft.Extensions.Logging;

namespace HotelTravel.Infrastructure.Services;

/// <summary>
/// Twilio Voice: POST /Accounts/{Sid}/Calls.json (To, From, Twiml).
/// Telefon zəng çalır, açanda mətn səslə oxunur. Paket lazım deyil - adi HttpClient.
/// </summary>
public class TwilioPhoneCallService : IPhoneCallService
{
    private readonly HttpClient _http;
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioPhoneCallService> _logger;

    public TwilioPhoneCallService(HttpClient http, TwilioOptions options, ILogger<TwilioPhoneCallService> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public bool IsEnabled => _options.IsEnabled;

    public async Task<(bool Success, string Message)> CallAsync(string toNumber, string spokenText, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return (false, "Phone calls are not configured. Add the Twilio settings to User Secrets.");

        // Demo: zəng öz telefonuna gəlsin
        var target = HotelCallScript.NormalizePhone(
            string.IsNullOrWhiteSpace(_options.TestPhoneOverride) ? toNumber : _options.TestPhoneOverride);

        if (target is null)
            return (false, "The phone number is not valid.");

        var url = $"{_options.BaseUrl.TrimEnd('/')}/Accounts/{_options.AccountSid}/Calls.json";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = target,
                ["From"] = _options.FromNumber!.Trim(),
                ["Twiml"] = HotelCallScript.Twiml(spokenText)
            })
        };

        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
                return (true, $"Calling {Mask(target)} now…");

            var error = ReadError(body);
            _logger.LogWarning("Twilio call failed ({Status}): {Error}", (int)response.StatusCode, error);

            return (false, $"The call could not be started: {error}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Twilio is not reachable");
            return (false, "The phone service is not reachable right now.");
        }
    }

    // Twilio xətası: { "code": 21219, "message": "The number ... is unverified..." }
    private static string ReadError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var m) && m.GetString() is { Length: > 0 } msg)
                return msg;
        }
        catch (JsonException)
        {
        }

        return "unknown error";
    }

    // +994501234567 -> +99450***4567 (log və mesajda tam nömrə görünməsin)
    private static string Mask(string phone) =>
        phone.Length <= 10 ? phone : phone[..6] + new string('*', phone.Length - 10) + phone[^4..];
}
