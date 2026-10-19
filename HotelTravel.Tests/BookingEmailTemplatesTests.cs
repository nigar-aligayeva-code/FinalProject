using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Enums;
using Xunit;

namespace HotelTravel.Tests;

public class BookingEmailTemplatesTests
{
    private static GetBookingDto Booking() => new()
    {
        ConfirmationCode = "BK-TEST1234",
        CheckInDate = new DateTime(2026, 11, 10),
        CheckOutDate = new DateTime(2026, 11, 13),
        AdultCount = 2,
        ChildrenCount = 1,
        TotalPrice = 450m,
        OriginalPrice = 500m,
        DiscountAmount = 50m,
        DiscountNote = "Promo WELCOME10",
        Status = BookingStatus.Confirmed,
        RoomName = "Deluxe Room",
        RoomNumber = "201",
        HotelName = "Caspian Grand Hotel",
        HotelCity = "Baku",
        HotelCountry = "Azerbaijan",
        GuestName = "Nigar Aliyeva",
        GuestEmail = "nigar@example.com"
    };

    [Fact]
    public void Confirmation_ShouldContainCodeHotelAndTotal()
    {
        var (subject, html) = BookingEmailTemplates.Confirmation(Booking(), "http://localhost:5233/Bookings/Find");

        Assert.Contains("BK-TEST1234", subject);
        Assert.Contains("Caspian Grand Hotel", subject);
        Assert.Contains("450.00 AZN", html);
        Assert.Contains("-50.00 AZN", html);
        Assert.Contains("Hi Nigar", html);
    }

    [Fact]
    public void Confirmation_ShouldEncodeUserInput()
    {
        var booking = Booking();
        booking.GuestName = "<script>alert(1)</script>";

        var (_, html) = BookingEmailTemplates.Confirmation(booking, "http://localhost");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Cancellation_ShouldMentionCancelled()
    {
        var (subject, html) = BookingEmailTemplates.Cancellation(Booking(), "http://localhost");

        Assert.StartsWith("Booking cancelled", subject);
        Assert.Contains("has been cancelled", html);
    }

    [Theory]
    [InlineData(1, 0, "1 adult")]
    [InlineData(2, 0, "2 adults")]
    [InlineData(2, 1, "2 adults, 1 child")]
    [InlineData(1, 3, "1 adult, 3 children")]
    public void Guests_ShouldPluralize(int adults, int children, string expected)
    {
        Assert.Equal(expected, BookingEmailTemplates.Guests(adults, children));
    }

    [Fact]
    public void Money_ShouldUseAzn()
    {
        Assert.Equal("1,234.50 AZN", BookingEmailTemplates.Money(1234.5m));
    }
}
