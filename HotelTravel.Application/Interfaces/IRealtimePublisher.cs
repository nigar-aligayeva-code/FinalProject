using HotelTravel.Application.DTOs.Notifications;

namespace HotelTravel.Application.Interfaces;

// Canlı mesajı çatdırır (API-də SignalR ilə implement olunur)
public interface IRealtimePublisher
{
    Task ToAdminsAsync(LiveNotificationDto notification);

    Task ToEveryoneAsync(PublicActivityDto activity);
}
