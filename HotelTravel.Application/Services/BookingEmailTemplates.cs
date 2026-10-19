using System.Globalization;
using System.Net;
using HotelTravel.Application.DTOs.Bookings;

namespace HotelTravel.Application.Services;

// Məktub mətnləri (HTML). İstifadəçi məlumatı həmişə HtmlEncode olunur.
public static class BookingEmailTemplates
{
    public const string ConfirmationType = "BookingConfirmation";
    public const string CancellationType = "BookingCancellation";

    private static readonly CultureInfo En = CultureInfo.InvariantCulture;

    public static (string Subject, string Html) Confirmation(GetBookingDto b, string bookingUrl)
    {
        var nights = Math.Max(1, (b.CheckOutDate.Date - b.CheckInDate.Date).Days);

        var subject = $"Booking confirmed: {b.HotelName} · {b.ConfirmationCode}";

        var discount = b.DiscountAmount > 0
            ? Row("Discount", $"-{Money(b.DiscountAmount)}" + (string.IsNullOrWhiteSpace(b.DiscountNote) ? "" : $" ({E(b.DiscountNote)})"))
            : "";

        var body = $@"
<p style=""margin:0 0 16px"">Hi {E(FirstName(b.GuestName))},</p>
<p style=""margin:0 0 20px"">Your stay is confirmed. Your voucher is attached as a PDF. Show it (or the QR code) at check-in.</p>

<div style=""background:#F3EEE5;border-radius:14px;padding:18px 20px;margin-bottom:20px;text-align:center"">
  <div style=""font-size:12px;letter-spacing:.12em;color:#52614e;font-weight:700"">CONFIRMATION CODE</div>
  <div style=""font-size:26px;font-weight:800;letter-spacing:.08em;color:#1F2421;margin-top:4px"">{E(b.ConfirmationCode)}</div>
</div>

<table style=""width:100%;border-collapse:collapse;font-size:14px"">
  {Row("Hotel", E(b.HotelName))}
  {Row("Address", E(JoinParts(b.HotelAddress, b.HotelCity, b.HotelCountry)))}
  {Row("Room", E($"{b.RoomName} (No. {b.RoomNumber})"))}
  {Row("Check-in", Date(b.CheckInDate) + " · from 14:00")}
  {Row("Check-out", Date(b.CheckOutDate) + " · until 12:00")}
  {Row("Guests", Guests(b.AdultCount, b.ChildrenCount))}
  {Row("Nights", nights.ToString(En))}
  {discount}
  {Row("<b>Total</b>", $"<b>{Money(b.TotalPrice)}</b>")}
</table>

<p style=""margin:24px 0"">
  <a href=""{E(bookingUrl)}"" style=""background:#1F2421;color:#fff;text-decoration:none;padding:12px 22px;border-radius:999px;font-weight:700;display:inline-block"">View or manage booking</a>
</p>
<p style=""margin:0;color:#6b7280;font-size:13px"">Free cancellation until the day before check-in.</p>";

        return (subject, Layout(subject, body));
    }

    public static (string Subject, string Html) Cancellation(GetBookingDto b, string bookingUrl)
    {
        var subject = $"Booking cancelled: {b.HotelName} · {b.ConfirmationCode}";

        var body = $@"
<p style=""margin:0 0 16px"">Hi {E(FirstName(b.GuestName))},</p>
<p style=""margin:0 0 20px"">Your booking <b>{E(b.ConfirmationCode)}</b> has been cancelled. No further action is needed.</p>

<table style=""width:100%;border-collapse:collapse;font-size:14px"">
  {Row("Hotel", E(b.HotelName))}
  {Row("Room", E(b.RoomName))}
  {Row("Dates", $"{Date(b.CheckInDate)} → {Date(b.CheckOutDate)}")}
</table>

<p style=""margin:24px 0 0"">We hope to see you soon. <a href=""{E(bookingUrl)}"" style=""color:#52614e;font-weight:700"">View booking</a></p>";

        return (subject, Layout(subject, body));
    }

    // =========================
    // HELPERS
    // =========================

    public static string Money(decimal amount)
        => amount.ToString("N2", En) + " AZN";

    public static string Guests(int adults, int children)
    {
        var text = $"{adults} adult{(adults == 1 ? "" : "s")}";

        if (children > 0)
            text += $", {children} child{(children == 1 ? "" : "ren")}";

        return text;
    }

    private static string Date(DateTime d) => d.ToString("ddd, dd MMM yyyy", En);

    private static string E(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);

    private static string FirstName(string? fullName)
        => string.IsNullOrWhiteSpace(fullName)
            ? "there"
            : fullName.Trim().Split(' ')[0];

    private static string JoinParts(params string?[] parts)
        => string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    // label və value artıq təhlükəsiz HTML-dir
    private static string Row(string label, string value)
        => $@"<tr><td style=""padding:9px 0;color:#6b7280;border-bottom:1px solid #eee;width:38%"">{label}</td><td style=""padding:9px 0;color:#1F2421;border-bottom:1px solid #eee"">{value}</td></tr>";

    private static string Layout(string title, string content) => $@"<!doctype html>
<html><head><meta charset=""utf-8""><title>{E(title)}</title></head>
<body style=""margin:0;background:#F3EEE5;font-family:Segoe UI,Arial,sans-serif;color:#1F2421"">
  <div style=""max-width:600px;margin:0 auto;padding:28px 16px"">
    <div style=""font-family:Georgia,serif;font-size:26px;font-weight:700;margin-bottom:18px"">&#11042; Tripio</div>
    <div style=""background:#fff;border-radius:18px;padding:28px"">{content}</div>
    <p style=""color:#9ca3af;font-size:12px;text-align:center;margin-top:18px"">This email was sent by Tripio. Please do not reply.</p>
  </div>
</body></html>";
}
