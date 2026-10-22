using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

public class CalendarRulesTests
{
    private static CalendarEvent Hotel() => CalendarRules.ForHotel(
        "BK-1", "Caspian Grand Hotel", "Neftchilar Ave 1", "Baku",
        new DateTime(2026, 11, 10), new DateTime(2026, 11, 13), "Deluxe Room");

    [Fact]
    public void ForHotel_ShouldUseCheckInAndCheckOutTimes()
    {
        var e = Hotel();

        Assert.Equal(new DateTime(2026, 11, 10, 14, 0, 0), e.Start);
        Assert.Equal(new DateTime(2026, 11, 13, 12, 0, 0), e.End);
    }

    [Fact]
    public void BuildIcs_ShouldBeValidCalendar()
    {
        var ics = CalendarRules.BuildIcs(Hotel());

        Assert.StartsWith("BEGIN:VCALENDAR\r\n", ics);
        Assert.Contains("DTSTART:20261110T140000", ics);
        Assert.Contains("DTEND:20261113T120000", ics);
        Assert.Contains("UID:BK-1@tripio", ics);
        Assert.EndsWith("END:VCALENDAR\r\n", ics);
    }

    [Fact]
    public void BuildIcs_ShouldEscapeCommas()
    {
        var ics = CalendarRules.BuildIcs(Hotel());

        Assert.Contains("LOCATION:Caspian Grand Hotel\\, Neftchilar Ave 1\\, Baku", ics);
    }

    [Fact]
    public void GoogleUrl_ShouldContainDatesAndTitle()
    {
        var url = CalendarRules.GoogleUrl(Hotel());

        Assert.Contains("dates=20261110T140000/20261113T120000", url);
        Assert.Contains("text=Stay%20at%20Caspian%20Grand%20Hotel", url);
    }

    [Fact]
    public void ForFlight_ShouldNeverEndBeforeStart()
    {
        var dep = new DateTime(2026, 11, 10, 23, 0, 0);
        var e = CalendarRules.ForFlight("FL-1", "AZAL", "J2 8", "Baku", "GYD", "Istanbul", "IST", dep, dep.AddHours(-1));

        Assert.True(e.End > e.Start);
    }
}
