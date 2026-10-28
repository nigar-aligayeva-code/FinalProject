using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Services;
using HotelTravel.Infrastructure.Seed;
using Xunit;

namespace HotelTravel.Tests;

// Otelə avtomatik zəng (Twilio) + test otaqlarının gizlədilməsi
public class HotelCallTests
{
    [Theory]
    [InlineData("+994 12 555-00-00", "+994125550000")]
    [InlineData("00994 50 123 45 67", "+994501234567")]
    [InlineData("050 123 45 67", "+994501234567")]   // yerli AZ formatı
    [InlineData("+90 (212) 555 01 01", "+902125550101")]
    public void NormalizePhone_ShouldReturnE164(string input, string expected)
        => Assert.Equal(expected, HotelCallScript.NormalizePhone(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    public void NormalizePhone_ShouldRejectInvalid(string? input)
        => Assert.Null(HotelCallScript.NormalizePhone(input));

    [Fact]
    public void Script_ShouldContainRequestDetails_AndTwimlShouldBeEscaped()
    {
        var text = HotelCallScript.For(new GetBookingDto
        {
            ConfirmationCode = "BK-AB12",
            HotelName = "Caspian & Grand",
            RoomNumber = "301",
            RoomName = "Deluxe Room",
            CheckInDate = new DateTime(2026, 11, 10),
            CheckOutDate = new DateTime(2026, 11, 12),
            AdultCount = 2,
            GuestName = "Nigar Aliyeva"
        });

        Assert.Contains("November 10", text);
        Assert.Contains("3 0 1", text);        // otaq nömrəsi rəqəm-rəqəm oxunur
        Assert.Contains("2 guests", text);

        var twiml = HotelCallScript.Twiml(text);
        Assert.StartsWith("<Response>", twiml);
        Assert.Contains("Caspian &amp; Grand", twiml);
    }

    [Theory]
    [InlineData("Test Room", true)]
    [InlineData("test", true)]
    [InlineData("TEST-1", true)]
    [InlineData("Testarossa Suite", false)]
    [InlineData("Deluxe Room", false)]
    public void LooksLikeTestRoom(string name, bool expected)
        => Assert.Equal(expected, TestDataCleanupSeeder.LooksLikeTestRoom(name));
}
