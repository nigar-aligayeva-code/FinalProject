using System.Globalization;
using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.DTOs.Notifications;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Options;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

/// <summary>
/// Xatırlatmalar:
///   • otel: girişə 3 gün qalmış və bir gün qalmış (yalnız təsdiqlənmiş rezervasiya);
///   • uçuş: 24 saat və 3 saat qalmış.
/// Hər xatırlatma e-poçtla gedir, qonaq saytdadırsa canlı bildiriş də görür.
/// Eyni xatırlatma iki dəfə getmir: EmailLog-da (Type + kod) artıq varsa keçilir.
/// </summary>
public class ReminderService : IReminderService
{
    private static readonly CultureInfo En = CultureInfo.InvariantCulture;

    private readonly IAppDbContext _context;
    private readonly IBookingService _bookings;
    private readonly IBookingEmailService _bookingEmails;
    private readonly IEmailOutbox _outbox;
    private readonly IActivityNotifier _notifier;
    private readonly EmailOptions _options;

    public ReminderService(
        IAppDbContext context,
        IBookingService bookings,
        IBookingEmailService bookingEmails,
        IEmailOutbox outbox,
        IActivityNotifier notifier,
        EmailOptions options)
    {
        _context = context;
        _bookings = bookings;
        _bookingEmails = bookingEmails;
        _outbox = outbox;
        _notifier = notifier;
        _options = options;
    }

    public async Task<int> SendDueAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var sent = 0;

        sent += await SendHotelRemindersAsync(now, cancellationToken);
        sent += await SendFlightRemindersAsync(now, cancellationToken);

        return sent;
    }

    // =========================
    // OTEL
    // =========================
    private async Task<int> SendHotelRemindersAsync(DateTime now, CancellationToken ct)
    {
        var from = now.Date.AddDays(1);
        var to = now.Date.AddDays(ReminderRules.HotelUpcomingDays);

        var candidates = await _context.Bookings
            .Where(b =>
                !b.IsDeleted &&
                b.Status == BookingStatus.Confirmed &&
                b.CheckInDate >= from &&
                b.CheckInDate <= to)
            .Select(b => new { b.ConfirmationCode, b.CheckInDate, b.AppUserId })
            .ToListAsync(ct);

        var sent = 0;

        foreach (var c in candidates)
        {
            var kind = ReminderRules.ForHotel(c.CheckInDate, now);
            var type = ReminderRules.EmailType(kind);

            if (kind == ReminderKind.None || await AlreadySentAsync(type, c.ConfirmationCode, ct))
                continue;

            var booking = await _bookings.GetByConfirmationCodeAsync(c.ConfirmationCode);

            if (booking == null)
                continue;

            var isTomorrow = kind == ReminderKind.HotelTomorrow;
            var (subject, html) = BookingEmailTemplates.HotelReminder(
                booking,
                _bookingEmails.GetBookingUrl(booking.ConfirmationCode),
                isTomorrow);

            await _outbox.SendAsync(new EmailMessage
            {
                To = booking.GuestEmail,
                Subject = subject,
                HtmlBody = html
            }, type, booking.ConfirmationCode);

            await _notifier.ToGuestAsync(c.AppUserId, new GuestNotificationDto
            {
                Kind = ReminderRules.NotificationKind(kind),
                Place = booking.HotelName ?? string.Empty,
                Code = booking.ConfirmationCode,
                When = booking.CheckInDate.ToString("dd MMM", En),
                Url = ActivityNotifier.BookingUrl(booking.ConfirmationCode),
                Message = subject
            });

            sent++;
        }

        return sent;
    }

    // =========================
    // UÇUŞ
    // =========================
    private async Task<int> SendFlightRemindersAsync(DateTime now, CancellationToken ct)
    {
        var until = now.AddHours(ReminderRules.FlightDayBeforeHours);

        var flights = await _context.FlightBookings
            .Where(f =>
                !f.IsDeleted &&
                f.Status == FlightBookingStatus.Confirmed &&
                f.DepartureTime > now &&
                f.DepartureTime <= until)
            .ToListAsync(ct);

        var sent = 0;

        foreach (var f in flights)
        {
            var kind = ReminderRules.ForFlight(f.DepartureTime, now);
            var type = ReminderRules.EmailType(kind);

            if (kind == ReminderKind.None || await AlreadySentAsync(type, f.ConfirmationCode, ct))
                continue;

            var (subject, html) = BookingEmailTemplates.FlightReminder(
                f.ContactName,
                f.ConfirmationCode,
                f.FlightNumber,
                f.Airline,
                f.FromCity,
                f.FromCode,
                f.ToCity,
                f.ToCode,
                f.DepartureTime,
                f.Passengers,
                kind == ReminderKind.FlightSoon,
                $"{_options.PublicSiteUrl.TrimEnd('/')}/Bookings/MyBookings");

            await _outbox.SendAsync(new EmailMessage
            {
                To = f.Email,
                Subject = subject,
                HtmlBody = html
            }, type, f.ConfirmationCode);

            await _notifier.ToGuestAsync(f.AppUserId, new GuestNotificationDto
            {
                Kind = ReminderRules.NotificationKind(kind),
                Place = f.ToCity,
                Code = f.ConfirmationCode,
                When = f.DepartureTime.ToString("HH:mm", En),
                Url = "/Bookings/MyBookings",
                Message = subject
            });

            sent++;
        }

        return sent;
    }

    private Task<bool> AlreadySentAsync(string type, string code, CancellationToken ct)
        => _context.EmailLogs.AnyAsync(e => e.Type == type && e.ReferenceCode == code, ct);
}
