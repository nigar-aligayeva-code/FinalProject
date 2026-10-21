using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Options;

namespace HotelTravel.Application.Services;

// Rezervasiya məktubları: mətn + PDF + çatdırma + jurnal.
// Xəta olsa rezervasiya pozulmur: nəticə qaytarılır və EmailLog-da qeyd olunur.
public class BookingEmailService : IBookingEmailService
{
    private readonly IBookingService _bookingService;
    private readonly IVoucherPdfService _pdfService;
    private readonly IEmailOutbox _outbox;
    private readonly EmailOptions _options;

    public BookingEmailService(
        IBookingService bookingService,
        IVoucherPdfService pdfService,
        IEmailOutbox outbox,
        EmailOptions options)
    {
        _bookingService = bookingService;
        _pdfService = pdfService;
        _outbox = outbox;
        _options = options;
    }

    public string GetBookingUrl(string confirmationCode)
        => $"{_options.PublicSiteUrl.TrimEnd('/')}/Bookings/Find?confirmationCode={Uri.EscapeDataString(confirmationCode)}";

    public async Task<(bool Success, string Message)> SendConfirmationAsync(string confirmationCode)
    {
        var booking = await _bookingService.GetByConfirmationCodeAsync(confirmationCode);

        if (booking == null)
            return (false, "Booking not found.");

        var url = GetBookingUrl(booking.ConfirmationCode);
        var (subject, html) = BookingEmailTemplates.Confirmation(booking, url);

        var message = new EmailMessage
        {
            To = booking.GuestEmail,
            Subject = subject,
            HtmlBody = html
        };

        try
        {
            message.Attachments.Add(new EmailAttachment
            {
                FileName = $"Tripio-{booking.ConfirmationCode}.pdf",
                ContentType = "application/pdf",
                Content = _pdfService.Create(booking, url)
            });
        }
        catch (Exception)
        {
            // PDF alınmasa da məktub göndərilsin
        }

        return await DeliverAsync(message, BookingEmailTemplates.ConfirmationType, booking.ConfirmationCode);
    }

    public async Task<(bool Success, string Message)> SendCancellationAsync(string confirmationCode)
    {
        var booking = await _bookingService.GetByConfirmationCodeAsync(confirmationCode);

        if (booking == null)
            return (false, "Booking not found.");

        var (subject, html) = BookingEmailTemplates.Cancellation(booking, GetBookingUrl(booking.ConfirmationCode));

        return await DeliverAsync(new EmailMessage
        {
            To = booking.GuestEmail,
            Subject = subject,
            HtmlBody = html
        }, BookingEmailTemplates.CancellationType, booking.ConfirmationCode);
    }

    private Task<(bool Success, string Message)> DeliverAsync(
        EmailMessage message,
        string type,
        string code)
        => _outbox.SendAsync(message, type, code);
}
