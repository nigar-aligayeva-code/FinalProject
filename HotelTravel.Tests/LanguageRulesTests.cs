using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

public class LanguageRulesTests
{
    [Theory]
    [InlineData("az", "az")]
    [InlineData("AZ", "az")]
    [InlineData(" ru ", "ru")]
    [InlineData("az-Latn-AZ", "az")]
    [InlineData("ru_RU", "ru")]
    [InlineData("en-US", "en")]
    [InlineData("de", "en")]
    [InlineData("", "en")]
    [InlineData(null, "en")]
    public void Normalize_ShouldReturnSupportedShortCode(string? input, string expected)
    {
        Assert.Equal(expected, LanguageRules.Normalize(input));
    }

    [Fact]
    public void Languages_ShouldContainThreeLanguages_WithEnglishAsDefault()
    {
        Assert.Equal(new[] { "en", "az", "ru" }, LanguageRules.Codes);
        Assert.Equal("English", LanguageRules.LabelOf("xx"));
        Assert.Equal("Azərbaycanca", LanguageRules.LabelOf("az"));
    }
}
