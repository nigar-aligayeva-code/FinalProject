using HotelTravel.Application.Services;
using HotelTravel.Domain.Enums;
using Xunit;

namespace HotelTravel.Tests;

public class BookingRulesTests
{
    private static readonly DateTime Today = new(2026, 10, 13);


    // =========================
    // CAN GUEST CHANGE (F4)
    // =========================
    [Fact]
    public void CanGuestChange_ShouldAllow_ConfirmedFutureBooking()
    {
        var error = BookingRules.CanGuestChange(
            BookingStatus.Confirmed,
            Today.AddDays(1),
            Today);

        Assert.Null(error);
    }

    [Fact]
    public void CanGuestChange_ShouldReject_OnCheckInDay()
    {
        var error = BookingRules.CanGuestChange(
            BookingStatus.Confirmed,
            Today,
            Today);

        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.CheckedOut)]
    [InlineData(BookingStatus.Cancelled)]
    public void CanGuestChange_ShouldReject_WhenNotConfirmed(
        BookingStatus status)
    {
        var error = BookingRules.CanGuestChange(
            status,
            Today.AddDays(5),
            Today);

        Assert.NotNull(error);
    }


    // =========================
    // VALIDATE STAY (F2 / F4)
    // =========================
    [Fact]
    public void ValidateStay_ShouldAccept_ValidStay()
    {
        var error = BookingRules.ValidateStay(
            Today.AddDays(2),
            Today.AddDays(5),
            adultCount: 2,
            childrenCount: 1,
            roomCapacity: 3,
            today: Today);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateStay_ShouldReject_PastCheckIn()
    {
        var error = BookingRules.ValidateStay(
            Today.AddDays(-1), Today.AddDays(2), 1, 0, 2, Today);

        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(0)]   // eyni gün
    [InlineData(-1)]  // check-out check-in-dən əvvəl
    public void ValidateStay_ShouldReject_InvalidDateRange(int extraDays)
    {
        var checkIn = Today.AddDays(3);

        var error = BookingRules.ValidateStay(
            checkIn, checkIn.AddDays(extraDays), 1, 0, 2, Today);

        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateStay_ShouldReject_TooManyGuests()
    {
        var error = BookingRules.ValidateStay(
            Today.AddDays(1), Today.AddDays(2), 2, 1, 2, Today);

        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateStay_ShouldReject_NoAdults()
    {
        var error = BookingRules.ValidateStay(
            Today.AddDays(1), Today.AddDays(2), 0, 1, 2, Today);

        Assert.NotNull(error);
    }


    // =========================
    // CALCULATE TOTAL (F6)
    // =========================
    [Fact]
    public void CalculateTotal_ShouldMultiplyNightsByPrice()
    {
        var total = BookingRules.CalculateTotal(
            175m,
            Today.AddDays(1),
            Today.AddDays(4));

        Assert.Equal(525m, total);
    }

    [Fact]
    public void CalculateTotal_ShouldIgnoreTimeOfDay()
    {
        var total = BookingRules.CalculateTotal(
            100m,
            Today.AddDays(1).AddHours(18),
            Today.AddDays(3).AddHours(9));

        Assert.Equal(200m, total);
    }

    [Fact]
    public void CalculateTotal_ShouldBeZero_ForInvalidRange()
    {
        var total = BookingRules.CalculateTotal(
            100m,
            Today.AddDays(3),
            Today.AddDays(3));

        Assert.Equal(0m, total);
    }
}
