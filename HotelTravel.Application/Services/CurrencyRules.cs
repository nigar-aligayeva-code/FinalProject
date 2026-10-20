using HotelTravel.Application.DTOs.Currency;

namespace HotelTravel.Application.Services;

// Valyuta qaydaları. Ödəniş həmişə AZN-dədir, digər valyutalar yalnız göstəriş üçündür.
public static class CurrencyRules
{
    public const string Base = "AZN";

    // Layihədəki şəhərlərin valyutaları
    public static readonly IReadOnlyList<CurrencyInfoDto> Supported = new[]
    {
        new CurrencyInfoDto { Code = "AZN", Symbol = "₼", Name = "Azerbaijani manat" },
        new CurrencyInfoDto { Code = "USD", Symbol = "$", Name = "US dollar" },
        new CurrencyInfoDto { Code = "EUR", Symbol = "€", Name = "Euro" },
        new CurrencyInfoDto { Code = "TRY", Symbol = "₺", Name = "Turkish lira" },
        new CurrencyInfoDto { Code = "GEL", Symbol = "₾", Name = "Georgian lari" },
        new CurrencyInfoDto { Code = "AED", Symbol = "AED", Name = "UAE dirham" }
    };

    // İnternet olmasa təxmini məzənnələr (1 AZN = ...).
    // AZN dollara bağlıdır: 1 USD = 1.70 AZN; AED də dollara bağlıdır: 1 USD = 3.6725 AED.
    public static readonly IReadOnlyDictionary<string, decimal> FallbackRates = new Dictionary<string, decimal>
    {
        ["AZN"] = 1m,
        ["USD"] = 0.5882m,
        ["EUR"] = 0.51m,
        ["TRY"] = 25.5m,
        ["GEL"] = 1.59m,
        ["AED"] = 2.1603m
    };

    public static bool IsSupported(string? code)
        => !string.IsNullOrWhiteSpace(code) &&
           Supported.Any(c => c.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string? code)
        => IsSupported(code) ? code!.Trim().ToUpperInvariant() : Base;

    // Canlı məzənnələrdən yalnız dəstəklənənləri götür; çatmayanı / səhvi təxmini ilə doldur
    public static Dictionary<string, decimal> Merge(IReadOnlyDictionary<string, decimal>? live)
    {
        var result = new Dictionary<string, decimal>();

        foreach (var currency in Supported)
        {
            result[currency.Code] =
                live != null && live.TryGetValue(currency.Code, out var rate) && rate > 0
                    ? rate
                    : FallbackRates[currency.Code];
        }

        result[Base] = 1m;

        return result;
    }

    public static decimal Convert(decimal amountInAzn, decimal rate)
        => Math.Round(amountInAzn * rate, 2, MidpointRounding.AwayFromZero);
}
