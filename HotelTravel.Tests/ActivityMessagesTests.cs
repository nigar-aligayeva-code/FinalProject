using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

public class ActivityMessagesTests
{
    [Fact]
    public void HotelBooked_ShouldDescribeStayForAdmins()
    {
        var n = ActivityMessages.HotelBooked(42, "BK-1", "Nigar A.", "Caspian Grand Hotel", "Baku", 3, 450m);

        Assert.Equal("hotel", n.Type);
        Assert.Equal("/Admin/Bookings/Details/42", n.Url);
        Assert.Contains("3 nights", n.Message);
        Assert.Contains("450.00 AZN", n.Message);
    }

    [Fact]
    public void HotelBooked_ShouldUseSingularNight()
    {
        var n = ActivityMessages.HotelBooked(1, "BK-1", "Guest", "Hotel", "Baku", 1, 100m);

        Assert.Contains("1 night ", n.Message);
    }

    [Theory]
    [InlineData("hotel", "Baku", "Someone just booked a stay in Baku")]
    [InlineData("flight", "Istanbul", "Someone just booked a flight to Istanbul")]
    [InlineData("trip", "Dubai", "Someone just booked a trip package to Dubai")]
    public void PublicFor_ShouldBeAnonymous(string type, string city, string expected)
    {
        Assert.Equal(expected, ActivityMessages.PublicFor(type, city).Message);
    }

    [Fact]
    public void PublicFor_ShouldNotLeakCodesOrNames()
    {
        var message = ActivityMessages.PublicFor("hotel", "Baku").Message;

        Assert.DoesNotContain("BK-", message);
        Assert.DoesNotContain("@", message);
    }

    [Theory]
    [InlineData("Nigar", "Aliyeva", "Nigar A.")]
    [InlineData("Nigar", "", "Nigar")]
    [InlineData("", "", "A guest")]
    [InlineData(null, null, "A guest")]
    public void ShortName_ShouldHideSurname(string? first, string? last, string expected)
    {
        Assert.Equal(expected, ActivityMessages.ShortName(first, last));
    }

    [Fact]
    public void TripBooked_ShouldShowDiscount()
    {
        var n = ActivityMessages.TripBooked(7, "TP-1", "Baku", "Istanbul", 2, 1200m, 12);

        Assert.Equal("trip", n.Type);
        Assert.Equal("/Admin/Bookings/Details/7", n.Url);
        Assert.Contains("-12%", n.Message);
    }

    [Fact]
    public void FlightBooked_ShouldHighlightRowInList()
    {
        var n = ActivityMessages.FlightBooked("FL-AB12", "Baku", "Dubai", 2, 500m);

        Assert.Equal("/Admin/Flights?highlight=FL-AB12", n.Url);
    }
}
