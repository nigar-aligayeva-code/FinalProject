using System.Globalization;
using System.Security;
using HotelTravel.Application.DTOs.Bookings;

namespace HotelTravel.Application.Services;

/// <summary>Otelə edilən avtomatik zəngdə oxunan mətn və nömrə təmizliyi (saf funksiyalar - test olunur).</summary>
public static class HotelCallScript
{
    /// <summary>"+994 (12) 555-00-00" -> "+994125550000". Yararsızdırsa null.</summary>
    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var trimmed = phone.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());

        if (trimmed.StartsWith("00"))
            digits = digits[2..];
        else if (!trimmed.StartsWith("+") && digits.StartsWith("0") && digits.Length == 10)
            digits = "994" + digits[1..]; // yerli AZ formatı: 050 123 45 67

        return digits.Length is >= 8 and <= 15 ? "+" + digits : null;
    }

    /// <summary>Telefonda oxunan mətn (ingilis dilində - Twilio səsi).</summary>
    public static string For(GetBookingDto b)
    {
        var inv = CultureInfo.InvariantCulture;
        var guests = b.AdultCount + b.ChildrenCount;

        return $"Hello, this is Tripio, the online booking service. " +
               $"We have a booking request for {b.HotelName}. " +
               $"Room {SpellNumber(b.RoomNumber)}, {b.RoomName}, " +
               $"from {b.CheckInDate.ToString("MMMM d", inv)} to {b.CheckOutDate.ToString("MMMM d", inv)}, " +
               $"for {guests} {(guests == 1 ? "guest" : "guests")}. " +
               $"Guest name: {b.GuestName}. " +
               $"Booking code: {SpellNumber(b.ConfirmationCode)}. " +
               "Please let the Tripio team know if this room is available. Thank you, goodbye.";
    }

    /// <summary>TwiML: mətn iki dəfə oxunur ki, telefonu gec açan da eşitsin.</summary>
    public static string Twiml(string text)
    {
        var safe = SecurityElement.Escape(text);
        return $"<Response><Say voice=\"alice\" language=\"en-US\">{safe}</Say><Pause length=\"1\"/><Say voice=\"alice\" language=\"en-US\">{safe}</Say></Response>";
    }

    // "BK-AB12" -> "B K, A B 1 2" - səsli oxunanda rəqəmlər tək-tək deyilir
    private static string SpellNumber(string? value) =>
        string.Join(" ", (value ?? string.Empty).Where(char.IsLetterOrDigit));
}
