using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

public class AnalyticsTests
{
    private static readonly DateTime Today = new(2026, 10, 18);

    [Fact]
    public void Percent_ShouldReturnZero_WhenTotalIsZero()
    {
        Assert.Equal(0m, AnalyticsCalculator.Percent(5, 0));
    }

    [Fact]
    public void Percent_ShouldRoundToOneDecimal()
    {
        Assert.Equal(33.3m, AnalyticsCalculator.Percent(1, 3));
    }

    [Fact]
    public void Occupancy_ShouldCountOnlyNightsInsidePeriod()
    {
        // 2 gecə dövrdən əvvəl, 3 gecə dövrün içində
        var stays = new[] { (Today.AddDays(-2), Today.AddDays(3)) };

        var result = AnalyticsCalculator.OccupancyPercent(stays, roomCount: 1, from: Today, days: 10);

        Assert.Equal(30m, result);
    }

    [Fact]
    public void Occupancy_ShouldIgnoreStaysOutsidePeriod()
    {
        var stays = new[]
        {
            (Today.AddDays(-10), Today.AddDays(-5)),
            (Today.AddDays(40), Today.AddDays(45))
        };

        var result = AnalyticsCalculator.OccupancyPercent(stays, roomCount: 2, from: Today, days: 30);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void Occupancy_ShouldReturnZero_WhenNoRooms()
    {
        var stays = new[] { (Today, Today.AddDays(2)) };

        Assert.Equal(0m, AnalyticsCalculator.OccupancyPercent(stays, 0, Today, 30));
    }

    [Fact]
    public void MonthRange_ShouldIncludePastCurrentAndFutureMonths()
    {
        var months = AnalyticsCalculator.MonthRange(Today, 5, 6);

        Assert.Equal(12, months.Count);
        Assert.Equal(new DateTime(2026, 5, 1), months.First());
        Assert.Equal(new DateTime(2027, 4, 1), months.Last());
    }

    [Fact]
    public void InMonth_ShouldMatchYearAndMonth()
    {
        Assert.True(AnalyticsCalculator.InMonth(new DateTime(2026, 10, 31), new DateTime(2026, 10, 1)));
        Assert.False(AnalyticsCalculator.InMonth(new DateTime(2025, 10, 5), new DateTime(2026, 10, 1)));
    }
}
