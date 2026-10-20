using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

public class CurrencyRulesTests
{
    [Theory]
    [InlineData("USD", true)]
    [InlineData("eur", true)]
    [InlineData("GBP", false)]
    [InlineData(null, false)]
    public void IsSupported_ShouldKnowProjectCurrencies(string? code, bool expected)
    {
        Assert.Equal(expected, CurrencyRules.IsSupported(code));
    }

    [Fact]
    public void Normalize_ShouldFallBackToAzn()
    {
        Assert.Equal("AZN", CurrencyRules.Normalize("XYZ"));
        Assert.Equal("TRY", CurrencyRules.Normalize(" try "));
    }

    [Fact]
    public void Merge_ShouldUseLiveRatesAndFillMissing()
    {
        var live = new Dictionary<string, decimal> { ["USD"] = 0.59m, ["EUR"] = 0m, ["JPY"] = 90m };

        var rates = CurrencyRules.Merge(live);

        Assert.Equal(1m, rates["AZN"]);
        Assert.Equal(0.59m, rates["USD"]);                          // canlı
        Assert.Equal(CurrencyRules.FallbackRates["EUR"], rates["EUR"]); // 0 -> təxmini
        Assert.Equal(CurrencyRules.FallbackRates["GEL"], rates["GEL"]); // yoxdur -> təxmini
        Assert.False(rates.ContainsKey("JPY"));                     // dəstəklənmir
    }

    [Fact]
    public void Merge_ShouldWorkOffline()
    {
        var rates = CurrencyRules.Merge(null);

        Assert.Equal(CurrencyRules.Supported.Count, rates.Count);
    }

    [Fact]
    public void Convert_ShouldRoundToCents()
    {
        // 1 USD = 1.70 AZN -> 170 AZN = 100 USD
        Assert.Equal(99.99m, CurrencyRules.Convert(170m, 0.5882m));
    }
}
