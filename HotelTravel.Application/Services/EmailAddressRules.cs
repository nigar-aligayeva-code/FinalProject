using System.Net.Mail;
using HotelTravel.Application.DTOs.Email;

namespace HotelTravel.Application.Services;

/// <summary>
/// E-poçtun "təmiz" qaydaları (şəbəkəsiz, test olunur):
/// format, birdəfəlik (temp-mail) domenlər və məşhur domenlərdə yazı səhvi.
/// MX (poçt serveri) yoxlaması Infrastructure-dakı DnsEmailAddressChecker-dədir.
/// </summary>
public static class EmailAddressRules
{
    // Birdəfəlik / saxta poçt xidmətləri (botlar və spam üçün istifadə olunur)
    private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com", "10minutemail.com", "10minutemail.net", "guerrillamail.com", "guerrillamail.net",
        "guerrillamail.org", "sharklasers.com", "grr.la", "yopmail.com", "yopmail.net", "temp-mail.org",
        "tempmail.com", "tempmail.net", "temp-mail.io", "tempmailo.com", "trashmail.com", "trashmail.de",
        "getnada.com", "nada.email", "dispostable.com", "maildrop.cc", "mailnesia.com", "mintemail.com",
        "throwawaymail.com", "fakeinbox.com", "fakemail.net", "emailondeck.com", "mohmal.com",
        "moakt.com", "mytemp.email", "spamgourmet.com", "spam4.me", "mailcatch.com", "inboxkitten.com",
        "burnermail.io", "33mail.com", "tempr.email", "discard.email", "mail.tm", "mailpoof.com",
        "emailfake.com", "tempinbox.com", "trash-mail.com", "jetable.org", "mailtemp.net", "1secmail.com",
        "1secmail.net", "dropmail.me", "linshiyouxiang.net", "harakirimail.com", "example.com", "test.com"
    };

    // Yazı səhvlərini tapmaq üçün məşhur domenlər
    private static readonly string[] PopularDomains =
    {
        "gmail.com", "yahoo.com", "outlook.com", "hotmail.com", "icloud.com", "mail.ru", "yandex.ru",
        "yandex.com", "live.com", "aol.com", "proton.me", "protonmail.com", "mail.az", "box.az", "inbox.ru",
        "list.ru", "bk.ru", "gmx.com", "msn.com"
    };

    public static bool IsDisposable(string domain) =>
        DisposableDomains.Contains(domain.Trim().TrimEnd('.'));

    /// <summary>"nigar@gmial.com" -> "nigar@gmail.com"; səhv yoxdursa null.</summary>
    public static string? SuggestCorrection(string email)
    {
        var at = email.LastIndexOf('@');
        if (at <= 0 || at == email.Length - 1)
            return null;

        var local = email[..at];
        var domain = email[(at + 1)..].ToLowerInvariant();

        if (PopularDomains.Contains(domain))
            return null;

        var best = PopularDomains
            .Select(d => (Domain: d, Distance: Distance(domain, d)))
            .OrderBy(x => x.Distance)
            .First();

        // 1-2 hərf fərqi -> böyük ehtimalla yazı səhvidir
        return best.Distance is > 0 and <= 2 ? $"{local}@{best.Domain}" : null;
    }

    /// <summary>Şəbəkəsiz yoxlama. Uğurludursa IsValid=true (MX hələ yoxlanmayıb).</summary>
    public static EmailCheckResult CheckOffline(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return EmailCheckResult.Invalid("Email is required.");

        email = email.Trim();

        if (!MailAddress.TryCreate(email, out var parsed) ||
            !string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase) ||
            !parsed.Host.Contains('.') ||
            parsed.Host.EndsWith('.'))
        {
            return EmailCheckResult.Invalid("Enter a valid email address.");
        }

        var suggestion = SuggestCorrection(email);

        if (IsDisposable(parsed.Host))
            return EmailCheckResult.Invalid("Temporary or disposable email addresses are not allowed. Please use your real email.");

        return new EmailCheckResult { IsValid = true, Suggestion = suggestion };
    }

    // Levenshtein məsafəsi: neçə hərf dəyişmək lazımdır
    private static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[a.Length, b.Length];
    }
}
