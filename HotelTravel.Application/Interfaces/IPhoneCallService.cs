namespace HotelTravel.Application.Interfaces;

/// <summary>İnternet üzərindən əsl telefon zəngi (Twilio). Telefon açılanda mətn səslə oxunur.</summary>
public interface IPhoneCallService
{
    bool IsEnabled { get; }

    Task<(bool Success, string Message)> CallAsync(string toNumber, string spokenText, CancellationToken cancellationToken = default);
}
