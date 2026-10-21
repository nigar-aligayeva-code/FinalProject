using System.Net;
using System.Net.Mail;
using System.Text;
using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Options;

namespace HotelTravel.Infrastructure.Services;

// SMTP verilibsə real göndərir; yoxdursa məktubu .eml faylı kimi qovluğa yazır
// (.eml faylını Outlook / Windows Mail ilə açmaq olar, PDF əlavə də içindədir)
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(EmailOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(EmailMessage message)
    {
        using var mail = new MailMessage
        {
            From = new MailAddress(_options.From, _options.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        mail.To.Add(message.To);

        foreach (var attachment in message.Attachments)
        {
            mail.Attachments.Add(new Attachment(
                new MemoryStream(attachment.Content),
                attachment.FileName,
                attachment.ContentType));
        }

        using var client = new SmtpClient();

        if (_options.UsesSmtp)
        {
            client.Host = _options.SmtpHost!;
            client.Port = _options.SmtpPort;
            client.EnableSsl = _options.EnableSsl;

            if (!string.IsNullOrWhiteSpace(_options.SmtpUser))
                client.Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword);
        }
        else
        {
            var directory = string.IsNullOrWhiteSpace(_options.PickupDirectory)
                ? Path.Combine(AppContext.BaseDirectory, "App_Data", "Emails")
                : _options.PickupDirectory;

            Directory.CreateDirectory(directory);

            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = directory;

            // Test rejimi: poçt proqramı olmadan da baxmaq üçün .html (+ .pdf) nüsxəsi
            await SaveReadableCopyAsync(directory, message);
        }

        await client.SendMailAsync(mail);
    }

    // "2026-10-21_23-24-05_Reset-your-Tripio-password.html" — brauzerdə açılır, linklər işləyir
    private static async Task SaveReadableCopyAsync(string directory, EmailMessage message)
    {
        var name = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{Slug(message.Subject)}";

        var header =
            "<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:13px;background:#fffbe6;" +
            "border-bottom:1px solid #f0e2a1;padding:10px 16px\">" +
            $"<b>To:</b> {WebUtility.HtmlEncode(message.To)} &nbsp; <b>Subject:</b> {WebUtility.HtmlEncode(message.Subject)}" +
            (message.Attachments.Count > 0
                ? $" &nbsp; <b>Attachment:</b> {WebUtility.HtmlEncode(string.Join(", ", message.Attachments.Select(a => a.FileName)))} (saved next to this file)"
                : "") +
            "</div>";

        await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), header + message.HtmlBody, Encoding.UTF8);

        foreach (var attachment in message.Attachments)
            await File.WriteAllBytesAsync(Path.Combine(directory, $"{name}_{attachment.FileName}"), attachment.Content);
    }

    private static string Slug(string text)
    {
        var chars = text.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = string.Join("-", new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));

        return slug.Length > 60 ? slug[..60] : slug;
    }
}
