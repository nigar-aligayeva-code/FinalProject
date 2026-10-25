using DnsClient;
using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HotelTravel.Infrastructure.Services;

/// <summary>
/// E-poçtun real olub-olmadığını yoxlayır:
///   1) format, birdəfəlik domen, yazı səhvi (EmailAddressRules — şəbəkəsiz);
///   2) domenin poçt serveri varmı: DNS-də MX qeydi (yoxdursa A qeydi, RFC 5321).
/// DNS cavab vermirsə (internet yoxdur) istifadəçi bloklanmır — HasMailServer = null.
/// Nəticə 6 saat yaddaşda saxlanılır.
/// </summary>
public class DnsEmailAddressChecker : IEmailAddressChecker
{
    private static readonly LookupClient Dns = new(new LookupClientOptions
    {
        Timeout = TimeSpan.FromSeconds(3),
        Retries = 1,
        UseCache = true
    });

    private readonly IMemoryCache _cache;
    private readonly ILogger<DnsEmailAddressChecker> _logger;

    public DnsEmailAddressChecker(IMemoryCache cache, ILogger<DnsEmailAddressChecker> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<EmailCheckResult> CheckAsync(string email, CancellationToken cancellationToken = default)
    {
        var result = EmailAddressRules.CheckOffline(email);

        if (!result.IsValid)
            return result;

        var domain = email.Trim()[(email.Trim().LastIndexOf('@') + 1)..].ToLowerInvariant();

        result.HasMailServer = await _cache.GetOrCreateAsync($"mx:{domain}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
            return await HasMailServerAsync(domain, cancellationToken);
        });

        if (result.HasMailServer == false)
        {
            return EmailCheckResult.Invalid(
                "This email domain cannot receive mail. Please check the address.",
                result.Suggestion);
        }

        return result;
    }

    private async Task<bool?> HasMailServerAsync(string domain, CancellationToken cancellationToken)
    {
        try
        {
            var mx = await Dns.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);
            if (mx.Answers.MxRecords().Any())
                return true;

            // MX yoxdursa poçt domenin özünə (A qeydi) göndərilə bilər
            var a = await Dns.QueryAsync(domain, QueryType.A, cancellationToken: cancellationToken);
            return a.Answers.ARecords().Any();
        }
        catch (Exception ex) when (ex is DnsResponseException or OperationCanceledException or System.Net.Sockets.SocketException)
        {
            _logger.LogWarning("MX lookup for {Domain} failed: {Message}", domain, ex.Message);
            return null;
        }
    }
}
