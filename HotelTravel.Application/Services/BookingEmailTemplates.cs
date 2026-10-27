using System.Globalization;
using System.Net;
using HotelTravel.Application.DTOs.Bookings;

namespace HotelTravel.Application.Services;

// Məktub mətnləri (HTML). İstifadəçi məlumatı həmişə HtmlEncode olunur.
public static class BookingEmailTemplates
{
    public const string ConfirmationType = "BookingConfirmation";
    public const string CancellationType = "BookingCancellation";
    public const string RequestReceivedType = "BookingRequestReceived";
    public const string RejectionType = "BookingRejected";
    public const string ExpiredType = "BookingRequestExpired";
    public const string HotelReminderType = "HotelReminder";             // 3 gün qalmış
    public const string HotelReminderTomorrowType = "HotelReminderTomorrow";
    public const string FlightReminderType = "FlightReminder24h";
    public const string FlightReminderSoonType = "FlightReminder3h";

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
<p style=""margin:0;color:#6b7280;font-size:13px"">{E(PolicyText(b.CheckInDate))}</p>";

        return (subject, Layout(subject, body));
    }

    public static (string Subject, string Html) Cancellation(GetBookingDto b, string bookingUrl, bool byHotel = false)
    {
        var subject = $"Booking cancelled: {b.HotelName} · {b.ConfirmationCode}";

        var intro = byHotel
            ? $"We are sorry — the hotel had to cancel your booking <b>{E(b.ConfirmationCode)}</b>. No cancellation fee is charged."
            : $"Your booking <b>{E(b.ConfirmationCode)}</b> has been cancelled. No further action is needed.";

        // Gec ləğv cəriməsi və karta qaytarılan pul
        var money = "";

        if (b.CancellationFee > 0)
            money += Row("Cancellation fee", Money(b.CancellationFee));

        if (b.RefundAmount > 0)
            money += Row("Refunded to your card", $"<b>{Money(b.RefundAmount)}</b>");

        var body = $@"
<p style=""margin:0 0 16px"">Hi {E(FirstName(b.GuestName))},</p>
<p style=""margin:0 0 20px"">{intro}</p>

<table style=""width:100%;border-collapse:collapse;font-size:14px"">
  {Row("Hotel", E(b.HotelName))}
  {Row("Room", E(b.RoomName))}
  {Row("Dates", $"{Date(b.CheckInDate)} → {Date(b.CheckOutDate)}")}
  {money}
</table>

<p style=""margin:24px 0 0"">We hope to see you soon. <a href=""{E(bookingUrl)}"" style=""color:#52614e;font-weight:700"">View booking</a></p>";

        return (subject, Layout(subject, body));
    }

    // Otel 24 saatda cavab vermədi: sorğu avtomatik bağlandı
    public static (string Subject, string Html) RequestExpired(GetBookingDto b, string siteUrl)
    {
        var subject = $"Booking request expired: {b.HotelName} · {b.ConfirmationCode}";
        var searchUrl = $"{(siteUrl ?? string.Empty).TrimEnd('/')}/Rooms/Search";

        var body = $@"
<p style=""margin:0 0 16px"">Hi {E(FirstName(b.GuestName))},</p>
<p style=""margin:0 0 20px""><b>{E(b.HotelName)}</b> did not answer your request <b>{E(b.ConfirmationCode)}</b> within {BookingRules.HotelAnswerHours} hours, so we closed it to keep your plans moving. You have not been charged.</p>

<table style=""width:100%;border-collapse:collapse;font-size:14px"">
  {Row("Hotel", E(b.HotelName))}
  {Row("Room", E(b.RoomName))}
  {Row("Dates", $"{Date(b.CheckInDate)} → {Date(b.CheckOutDate)}")}
</table>

<p style=""margin:24px 0"">
  <a href=""{E(searchUrl)}"" style=""background:#1F2421;color:#fff;text-decoration:none;padding:12px 22px;border-radius:999px;font-weight:700;display:inline-block"">Find another room</a>
</p>";

        return (subject, Layout(subject, body));
    }

    // Xatırlatma: giriş yaxınlaşır (3 gün qalmış və bir gün qalmış)
    public static (string Subject, string Html) HotelReminder(GetBookingDto b, string bookingUrl, bool isTomorrow)
    {
        var when = isTomorrow ? "tomorrow" : $"on {Date(b.CheckInDate)}";
        var subject = $"Reminder: your stay at {b.HotelName} starts {when}";

        var pay = b.PaymentStatus == Domain.Enums.PaymentStatus.Paid
            ? "Your stay is already paid."
            : $"Total to pay: <b>{Money(b.TotalPrice)}</b>.";

        var body = $@"
<p style=""margin:0 0 16px"">Hi {E(FirstName(b.GuestName))},</p>
<p style=""margin:0 0 20px"">Your stay at <b>{E(b.HotelName)}</b> starts <b>{when}</b>. Check-in is from {CancellationPolicy.CheckInHour}:00. {pay}</p>

<table style=""width:100%;border-collapse:collapse;font-size:14px"">
  {Row("Confirmation code", $"<b>{E(b.ConfirmationCode)}</b>")}
  {Row("Address", E(JoinParts(b.HotelAddress, b.HotelCity, b.HotelCountry)))}
  {Row("Room", E($"{b.RoomName} (No. {b.RoomNumber})"))}
  {Row("Check-in", Date(b.CheckInDate))}
  {Row("Check-out", Date(b.CheckOutDate))}
</table>

<p style=""margin:24px 0"">
  <a href=""{E(bookingUrl)}"" style=""background:#1F2421;color:#fff;text-decoration:none;padding:12px 22px;border-radius:999px;font-weight:700;display:inline-block"">View booking</a>
</p>
<p style=""margin:0;color:#6b7280;font-size:13px"">{E(PolicyText(b.CheckInDate))}</p>";

        return (subject, Layout(subject, body));
    }

    // Xatırlatma: uçuşa 24 saat və 3 saat qalmış
    public static (string Subject, string Html) FlightReminder(
        string contactName,
        string confirmationCode,
        string flightNumber,
        string airline,
        string fromCity,
        string fromCode,
        string toCity,
        string toCode,
        DateTime departure,
        int passengers,
        bool isSoon,
        string bookingUrl)
    {
        var subject = isSoon
            ? $"Your flight {flightNumber} to {toCity} departs in a few hours"
            : $"Reminder: flight {flightNumber} to {toCity} on {Date(departure)} at {departure.ToString("HH:mm", En)}";

        var tip = isSoon
            ? "Please be at the airport at least 2 hours before departure. Check-in desks close 45 minutes before the flight."
            : "Online check-in is usually open now. Check your passport and luggage allowance.";

        var body = $@"
<p style=""margin:0 0 16px"">Hi {E(FirstName(contactName))},</p>
<p style=""margin:0 0 20px"">Your flight <b>{E(flightNumber)}</b> ({E(airline)}) from {E(fromCity)} to {E(toCity)} departs on <b>{Date(departure)} at {departure.ToString("HH:mm", En)}</b>.</p>

<table style=""width:100%;border-collapse:collapse;font-size:14px"">
  {Row("Booking code", $"<b>{E(confirmationCode)}</b>")}
  {Row("Route", E($"{fromCode} → {toCode}"))}
  {Row("Passengers", passengers.ToString(En))}
</table>

<p style=""margin:20px 0"">{E(tip)}</p>
<p style=""margin:0""><a href=""{E(bookingUrl)}"" style=""color:#52614e;font-weight:700"">My bookings</a></p>";

        return (subject, Layout(subject, body));
    }

    // Ləğv qaydası mətni (məktublarda)
    public static string PolicyText(DateTime checkInDate) =>
        $"Free cancellation until {Date(CancellationPolicy.HotelFreeUntil(checkInDate))} {CancellationPolicy.CheckInHour}:00. " +
        $"After that a {CancellationPolicy.LateFeePercent}% fee applies, and {CancellationPolicy.LastMinuteFeePercent}% in the last {CancellationPolicy.LateHours} hours.";

    // Sorğu qəbul olundu: otel hələ təsdiqləməyib, ödəniş də hələ yoxdur
    public static (string Subject, string Html) RequestReceived(GetBookingDto b, string bookingUrl)
    {
        var subject = $"Booking request received: {b.HotelName} · {b.ConfirmationCode}";

        var body = $@"
<p style=""margin:0 0 16px"">Hi {E(FirstName(b.GuestName))},</p>
<p style=""margin:0 0 20px"">Thanks! We have sent your request to <b>{E(b.HotelName)}</b>. The hotel checks its real availability and answers within <b>{BookingRules.HotelAnswerHours} hours</b>. We will email you as soon as it is confirmed. Nothing is charged until then.</p>

<div style=""background:#FFF7E6;border-radius:14px;padding:18px 20px;margin-bottom:20px;text-align:center"">
  <div style=""font-size:12px;letter-spacing:.12em;color:#92600a;font-weight:700"">AWAITING HOTEL CONFIRMATION</div>
  <div style=""font-size:26px;font-weight:800;letter-spacing:.08em;color:#1F2421;margin-top:4px"">{E(b.ConfirmationCode)}</div>
</div>

<table style=""width:100%;border-collapse:collapse;font-size:14px"">
  {Row("Hotel", E(b.HotelName))}
  {Row("Room", E($"{b.RoomName} (No. {b.RoomNumber})"))}
  {Row("Check-in", Date(b.CheckInDate))}
  {Row("Check-out", Date(b.CheckOutDate))}
  {Row("Guests", Guests(b.AdultCount, b.ChildrenCount))}
  {Row("<b>Total</b>", $"<b>{Money(b.TotalPrice)}</b>")}
</table>

<p style=""margin:24px 0"">
  <a href=""{E(bookingUrl)}"" style=""background:#1F2421;color:#fff;text-decoration:none;padding:12px 22px;border-radius:999px;font-weight:700;display:inline-block"">Check request status</a>
</p>
<p style=""margin:0;color:#6b7280;font-size:13px"">You can cancel the request at any time before check-in.</p>";

        return (subject, Layout(subject, body));
    }

    // Otel rədd etdi: o tarixlərdə boş otaq yoxdur
    public static (string Subject, string Html) Rejection(GetBookingDto b, string siteUrl)
    {
        var subject = $"Booking request declined: {b.HotelName} · {b.ConfirmationCode}";
        var searchUrl = $"{(siteUrl ?? string.Empty).TrimEnd('/')}/Rooms/Search";

        var body = $@"
<p style=""margin:0 0 16px"">Hi {E(FirstName(b.GuestName))},</p>
<p style=""margin:0 0 20px"">Unfortunately <b>{E(b.HotelName)}</b> has no free room of this type for your dates, so your request <b>{E(b.ConfirmationCode)}</b> was declined. You have not been charged.</p>

<table style=""width:100%;border-collapse:collapse;font-size:14px"">
  {Row("Hotel", E(b.HotelName))}
  {Row("Room", E(b.RoomName))}
  {Row("Dates", $"{Date(b.CheckInDate)} → {Date(b.CheckOutDate)}")}
</table>

<p style=""margin:24px 0"">
  <a href=""{E(searchUrl)}"" style=""background:#1F2421;color:#fff;text-decoration:none;padding:12px 22px;border-radius:999px;font-weight:700;display:inline-block"">Find another room</a>
</p>";

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
