using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Options;
using HotelTravel.Domain.Entities;

namespace HotelTravel.Application.Services;

// Rezervasiya məktubları: mətn + PDF + çatdırma + jurnal.
// Xəta olsa rezervasiya pozulmur: nəticə qaytarılır və EmailLog-da qeyd olunur.
public class BookingEmailService : IBookingEmailService
{
    private readonly IBookingService _bookingService;
    private readonly IVoucherPdfService _pdfService;
    private readonly IEmailSender _emailSender;
    private readonly IAppDbContext _context;
    private readonly EmailOptions _options;

    public BookingEmailService(
        IBookingService bookingService,
        IVoucherPdfService pdfService,
        IEmailSender emailSender,
        IAppDbContext context,
        EmailOptions options)
    {
        _bookingService = bookingService;
        _pdfService = pdfService;
        _emailSender = emailSender;
        _context = context;
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

    private async Task<(bool Success, string Message)> DeliverAsync(
        EmailMessage message,
        string type,
        string code)
    {
        var log = new EmailLog
        {
            To = message.To,
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            Type = type,
            ReferenceCode = code,
            HasAttachment = message.Attachments.Count > 0,
            CreatedAt = DateTime.Now
        };

        try
        {
            if (string.IsNullOrWhiteSpace(message.To))
                throw new InvalidOperationException("The booking has no email address.");

            await _emailSender.SendAsync(message);
            log.Delivered = true;
        }
        catch (Exception ex)
        {
            log.Delivered = false;
            log.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
        }

        _context.EmailLogs.Add(log);
        await _context.SaveChangesAsync();

        return log.Delivered
            ? (true, $"Email sent to {message.To}.")
            : (false, "The email could not be sent: " + log.Error);
    }
}
