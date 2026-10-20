using HotelTravel.Application.DTOs.Weather;
using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

public class WeatherRulesTests
{
    private static readonly DateTime Today = new(2026, 10, 20);

    [Fact]
    public void Window_ShouldDefaultToNextSevenDays()
    {
        var w = WeatherRules.GetWindow(null, null, Today)!;

        Assert.True(w.IsForecast);
        Assert.Equal(Today, w.Start);
        Assert.Equal(Today.AddDays(6), w.End);
    }

    [Fact]
    public void Window_ShouldUseForecast_ForTripWithinSixteenDays()
    {
        var w = WeatherRules.GetWindow(Today.AddDays(3), Today.AddDays(7), Today)!;

        Assert.True(w.IsForecast);
        Assert.Equal(0, w.YearShift);
    }

    [Fact]
    public void Window_ShouldCutForecastAtSixteenthDay()
    {
        var w = WeatherRules.GetWindow(Today.AddDays(12), Today.AddDays(20), Today)!;

        Assert.True(w.IsForecast);
        Assert.Equal(Today.AddDays(15), w.End);
    }

    [Fact]
    public void Window_ShouldUseLastYear_ForFarTrips()
    {
        var w = WeatherRules.GetWindow(Today.AddDays(60), Today.AddDays(64), Today)!;

        Assert.False(w.IsForecast);
        Assert.Equal(1, w.YearShift);
        Assert.Equal(Today.AddDays(60).AddYears(-1), w.Start);
    }

    [Fact]
    public void Window_ShouldBeNull_ForPastTrips()
    {
        Assert.Null(WeatherRules.GetWindow(Today.AddDays(-10), Today.AddDays(-5), Today));
    }

    [Fact]
    public void Window_ShouldLimitToFourteenDays()
    {
        var w = WeatherRules.GetWindow(Today.AddDays(100), Today.AddDays(130), Today)!;

        Assert.Equal(13, (w.End - w.Start).Days);
    }

    [Theory]
    [InlineData(0, "Clear sky")]
    [InlineData(63, "Rain")]
    [InlineData(95, "Thunderstorm")]
    [InlineData(1234, "Mixed")]
    public void Describe_ShouldMapWmoCodes(int code, string expected)
    {
        Assert.Equal(expected, WeatherRules.Describe(code).Summary);
    }

    [Fact]
    public void Advice_ShouldSuggestUmbrella_WhenMostlyRainy()
    {
        var days = new List<WeatherDayDto>
        {
            new() { Code = 61, TempMax = 18, TempMin = 12 },
            new() { Code = 63, TempMax = 17, TempMin = 11 },
            new() { Code = 1, TempMax = 20, TempMin = 13 }
        };

        Assert.Contains("umbrella", WeatherRules.GetAdvice(days));
    }

    [Fact]
    public void Advice_ShouldWarnAboutHeat()
    {
        var days = new List<WeatherDayDto>
        {
            new() { Code = 0, TempMax = 36, TempMin = 28 },
            new() { Code = 0, TempMax = 38, TempMin = 29 }
        };

        Assert.Contains("sunscreen", WeatherRules.GetAdvice(days));
    }
}
