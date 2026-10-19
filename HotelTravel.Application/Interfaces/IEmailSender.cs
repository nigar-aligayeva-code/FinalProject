using HotelTravel.Application.DTOs.Email;

namespace HotelTravel.Application.Interfaces;

// Məktubu çatdırır (SMTP və ya .eml faylı) - Infrastructure-da
public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}
