namespace HotelTravel.Application.Interfaces;

// Giriş və uçuş xatırlatmaları (API-dəki fon xidməti hər bir neçə dəqiqədən bir çağırır)
public interface IReminderService
{
    // Vaxtı çatmış xatırlatmaları göndərir, göndərilən məktubların sayını qaytarır
    Task<int> SendDueAsync(DateTime now, CancellationToken cancellationToken = default);
}
