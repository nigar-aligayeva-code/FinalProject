using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

public class ProfileRulesTests
{
    [Theory]
    [InlineData(0, 0, ProfileRules.Explorer)]
    [InlineData(2, 1, ProfileRules.Explorer)]
    [InlineData(3, 1, ProfileRules.Traveler)]
    [InlineData(1, 2, ProfileRules.Traveler)]
    [InlineData(8, 1, ProfileRules.Globetrotter)]
    [InlineData(2, 5, ProfileRules.Globetrotter)]
    public void GetLevel_ShouldDependOnTripsAndCities(int trips, int cities, string expected)
    {
        Assert.Equal(expected, ProfileRules.GetLevel(trips, cities));
    }

    [Fact]
    public void TripsToNextLevel_ShouldCountToTraveler_ForExplorer()
    {
        Assert.Equal(2, ProfileRules.TripsToNextLevel(1, 1));
    }

    [Fact]
    public void TripsToNextLevel_ShouldCountToGlobetrotter_ForTraveler()
    {
        Assert.Equal(4, ProfileRules.TripsToNextLevel(4, 1));
    }

    [Fact]
    public void TripsToNextLevel_ShouldBeZero_AtTopLevel()
    {
        Assert.Equal(0, ProfileRules.TripsToNextLevel(10, 6));
    }
}
