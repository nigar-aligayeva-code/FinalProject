using System.Text;

namespace HotelTravel.Application.Services;

public record CalendarEvent(
    string Uid,
    string Title,
    DateTime Start,
    DateTime End,
    string Location,
    string Description);

// "Add to calendar": .ics faylı (Outlook, Apple, telefon) və Google Calendar linki.
// Vaxtlar yerli vaxtdır (otelin / hava limanının saatı ilə).
public static class CalendarRules
{
    public const string CheckInTime = "14:00";
    public const string CheckOutTime = "12:00";

    public static CalendarEvent ForHotel(string code, string hotel, string? address, string? city,
        DateTime checkIn, DateTime checkOut, string room)
        => new(
            $"{code}@tripio",
            $"Stay at {hotel}",
            checkIn.Date.AddHours(14),
            checkOut.Date.AddHours(12),
            string.Join(", ", new[] { hotel, address, city }.Where(p => !string.IsNullOrWhiteSpace(p))),
            $"Tripio booking {code}. Room: {room}. Check-in from {CheckInTime}, check-out until {CheckOutTime}.");

    public static CalendarEvent ForFlight(string code, string airline, string flightNumber,
        string fromCity, string fromCode, string toCity, string toCode, DateTime departure, DateTime arrival)
        => new(
            $"{code}@tripio",
            $"Flight {flightNumber} {fromCode} → {toCode}",
            departure,
            arrival > departure ? arrival : departure.AddHours(1),
            $"{fromCity} ({fromCode})",
            $"{airline} {flightNumber}: {fromCity} → {toCity}. Tripio booking {code}. Times are local airport times.");

    public static string BuildIcs(CalendarEvent e)
    {
        var sb = new StringBuilder();

        sb.Append("BEGIN:VCALENDAR\r\n");
        sb.Append("VERSION:2.0\r\n");
        sb.Append("PRODID:-//Tripio//Booking//EN\r\n");
        sb.Append("CALSCALE:GREGORIAN\r\n");
        sb.Append("BEGIN:VEVENT\r\n");
        sb.Append($"UID:{Escape(e.Uid)}\r\n");
        sb.Append($"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}\r\n");
        sb.Append($"DTSTART:{Format(e.Start)}\r\n");
        sb.Append($"DTEND:{Format(e.End)}\r\n");
        sb.Append($"SUMMARY:{Escape(e.Title)}\r\n");
        sb.Append($"LOCATION:{Escape(e.Location)}\r\n");
        sb.Append($"DESCRIPTION:{Escape(e.Description)}\r\n");
        sb.Append("BEGIN:VALARM\r\nTRIGGER:-P1D\r\nACTION:DISPLAY\r\nDESCRIPTION:Reminder\r\nEND:VALARM\r\n");
        sb.Append("END:VEVENT\r\n");
        sb.Append("END:VCALENDAR\r\n");

        return sb.ToString();
    }

    public static string GoogleUrl(CalendarEvent e)
        => "https://calendar.google.com/calendar/render?action=TEMPLATE" +
           $"&text={Uri.EscapeDataString(e.Title)}" +
           $"&dates={Format(e.Start)}/{Format(e.End)}" +
           $"&details={Uri.EscapeDataString(e.Description)}" +
           $"&location={Uri.EscapeDataString(e.Location)}";

    // Yerli vaxt ("floating"): 20261110T140000
    private static string Format(DateTime value) => value.ToString("yyyyMMdd'T'HHmmss");

    // iCalendar qaydası: \ ; , və sətir sonu escape olunur
    private static string Escape(string text)
        => (text ?? string.Empty)
            .Replace("\\", "\\\\")
           .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
}
