using HotelTravel.Application.Services;
using HotelTravel.Domain.Enums;
using Xunit;

namespace HotelTravel.Tests;

public class TripPackageTests
{
    private static readonly DateTime Today = new(2026, 10, 19);

    [Theory]
    [InlineData(false, false, 8)]
    [InlineData(true, false, 10)]
    [InlineData(false, true, 10)]
    [InlineData(true, true, 12)]
    public void DiscountPercent_ShouldGrowWithParts(bool hasReturn, bool hasTaxi, int expected)
    {
        Assert.Equal(expected, TripPackageRules.GetDiscountPercent(hasReturn, hasTaxi));
    }

    [Fact]
    public void DiscountAmount_ShouldRoundToCents()
    {
        Assert.Equal(123.46m, TripPackageRules.GetDiscountAmount(1028.80m, 12));
    }

    [Fact]
    public void DiscountAmount_ShouldBeZero_ForEmptyTrip()
    {
        Assert.Equal(0m, TripPackageRules.GetDiscountAmount(0m, 12));
    }

    [Fact]
    public void TaxiPickup_ShouldWaitAfterLanding()
    {
        var arrival = new DateTime(2026, 11, 1, 14, 30, 0);

        Assert.Equal(new DateTime(2026, 11, 1, 15, 15, 0), TripPackageRules.GetTaxiPickup(arrival));
    }

    [Theory]
    [InlineData(1, TaxiVehicleType.Standard)]
    [InlineData(2, TaxiVehicleType.Standard)]
    [InlineData(3, TaxiVehicleType.Comfort)]
    [InlineData(5, TaxiVehicleType.Minivan)]
    public void ChooseVehicle_ShouldPickCheapestThatFits(int travelers, TaxiVehicleType expected)
    {
        Assert.Equal(expected, TripPackageRules.ChooseVehicle(travelers));
    }

    [Fact]
    public void ValidateSearch_ShouldAcceptNormalTrip()
    {
        Assert.Null(TripPackageRules.ValidateSearch("Baku", "Istanbul", Today.AddDays(10), Today.AddDays(14), 2, Today));
    }

    [Fact]
    public void ValidateSearch_ShouldRejectSameCity()
    {
        Assert.NotNull(TripPackageRules.ValidateSearch("Baku", "baku", Today.AddDays(10), Today.AddDays(14), 2, Today));
    }

    [Fact]
    public void ValidateSearch_ShouldRejectTripStartingToday()
    {
        Assert.NotNull(TripPackageRules.ValidateSearch("Baku", "Dubai", Today, Today.AddDays(3), 2, Today));
    }

    [Fact]
    public void ValidateSearch_ShouldRejectReturnBeforeDeparture()
    {
        Assert.NotNull(TripPackageRules.ValidateSearch("Baku", "Dubai", Today.AddDays(5), Today.AddDays(5), 2, Today));
    }

    [Fact]
    public void ValidateSearch_ShouldRejectTooManyTravelers()
    {
        Assert.NotNull(TripPackageRules.ValidateSearch("Baku", "Dubai", Today.AddDays(5), Today.AddDays(8), 7, Today));
    }
}
