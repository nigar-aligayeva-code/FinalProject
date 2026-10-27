namespace HotelTravel.Application.Interfaces;

// Otel 24 saatda cavab verməyən sorğuları avtomatik bağlayır (fon xidməti çağırır)
public interface IRequestExpiryService
{
    // Bağlanan sorğuların sayı
    Task<int> ExpireOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
