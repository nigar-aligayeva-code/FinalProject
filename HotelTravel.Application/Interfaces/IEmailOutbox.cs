using HotelTravel.Application.DTOs.Email;

namespace HotelTravel.Application.Interfaces;

// Məktubu göndərir və EmailLog-a yazır (Admin → Emails). Heç vaxt xəta atmır.
public interface IEmailOutbox
{
    Task<(bool Success, string Message)> SendAsync(EmailMessage message, string type, string? referenceCode);
}
