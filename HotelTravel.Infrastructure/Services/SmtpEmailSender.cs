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
        }

        await client.SendMailAsync(mail);
    }
}
