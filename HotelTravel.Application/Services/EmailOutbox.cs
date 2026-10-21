using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;

namespace HotelTravel.Application.Services;

// Bütün məktublar buradan keçir: çatdırma (IEmailSender) + jurnal (EmailLog)
public class EmailOutbox : IEmailOutbox
{
    private readonly IEmailSender _emailSender;
    private readonly IAppDbContext _context;

    public EmailOutbox(IEmailSender emailSender, IAppDbContext context)
    {
        _emailSender = emailSender;
        _context = context;
    }

    public async Task<(bool Success, string Message)> SendAsync(EmailMessage message, string type, string? referenceCode)
    {
        var log = new EmailLog
        {
            To = message.To,
            Subject = message.Subject,
            HtmlBody = message.LogHtmlBody ?? message.HtmlBody,
            Type = type,
            ReferenceCode = referenceCode,
            HasAttachment = message.Attachments.Count > 0,
            CreatedAt = DateTime.Now
        };

        try
        {
            if (string.IsNullOrWhiteSpace(message.To))
                throw new InvalidOperationException("There is no email address.");

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
