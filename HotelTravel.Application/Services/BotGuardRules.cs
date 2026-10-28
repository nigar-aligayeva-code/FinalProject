namespace HotelTravel.Application.Services;

/// <summary>
/// Formları botlardan qorumaq üçün sadə qaydalar (xarici servis lazım deyil):
///   • honeypot: insan görmədiyi gizli sahə - doludursa, botdur;
///   • vaxt: form 3 saniyədən tez göndərilirsə, botdur; 2 saatdan köhnədirsə, yenilənməlidir;
///   • eyni email-ə 2 dəqiqədə bir dəfədən çox bərpa məktubu getmir (məktub bombardmanı olmasın).
/// </summary>
public static class BotGuardRules
{
    public static readonly TimeSpan MinFillTime = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan MaxFormAge = TimeSpan.FromHours(2);
    public static readonly TimeSpan ResetEmailCooldown = TimeSpan.FromMinutes(2);

    public static bool LooksHuman(string? honeypot, DateTime? formIssuedUtc, DateTime nowUtc)
    {
        if (!string.IsNullOrEmpty(honeypot))
            return false;

        if (formIssuedUtc is null)
            return false;

        var age = nowUtc - formIssuedUtc.Value;

        return age >= MinFillTime && age <= MaxFormAge;
    }

    /// <summary>"nigaraligayeva@gmail.com" -> "ni***@gmail.com": istifadəçi yazdığı ünvanı görüb səhvi tuta bilsin.</summary>
    public static string MaskEmail(string? email)
    {
        var value = (email ?? string.Empty).Trim();
        var at = value.IndexOf('@');

        if (at <= 0)
            return value;

        var name = value[..at];
        var visible = name.Length <= 2 ? name[..1] : name[..2];

        return visible + "***" + value[at..];
    }
}
