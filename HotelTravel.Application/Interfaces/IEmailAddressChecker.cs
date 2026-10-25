using HotelTravel.Application.DTOs.Email;

namespace HotelTravel.Application.Interfaces;

/// <summary>Qeydiyyatda e-poçtun real olub-olmadığını yoxlayır (saxta/birdəfəlik/yazı səhvi/MX).</summary>
public interface IEmailAddressChecker
{
    Task<EmailCheckResult> CheckAsync(string email, CancellationToken cancellationToken = default);
}
