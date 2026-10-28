using System.Globalization;
using HotelTravel.Application.Services;
using Microsoft.AspNetCore.DataProtection;

namespace HotelTravel.Web.Services;

/// <summary>
/// Formun açılma vaxtı şifrələnmiş gizli sahədə saxlanılır (bot onu saxtalaşdıra bilmir).
/// Yoxlama qaydaları: BotGuardRules.
/// </summary>
public class BotGuard
{
    public const string TimeField = "__ft";
    public const string HoneypotField = "website";

    private readonly IDataProtector _protector;

    public BotGuard(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Tripio.BotGuard.v1");
    }

    public string Issue() =>
        _protector.Protect(DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));

    public bool IsHuman(IFormCollection form)
    {
        DateTime? issued = null;

        try
        {
            var raw = _protector.Unprotect(form[TimeField].ToString());

            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
                issued = new DateTime(ticks, DateTimeKind.Utc);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // saxta və ya boş dəyər
        }

        return BotGuardRules.LooksHuman(form[HoneypotField].ToString(), issued, DateTime.UtcNow);
    }
}
