using HotelTravel.API.Hubs;
using HotelTravel.Application.DTOs.Notifications;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace HotelTravel.API.Services;

// IRealtimePublisher -> SignalR
public class SignalRRealtimePublisher : IRealtimePublisher
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRRealtimePublisher(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public Task ToAdminsAsync(LiveNotificationDto notification)
        => _hub.Clients.Group(NotificationHub.AdminsGroup).SendAsync("adminNotification", notification);

    public Task ToEveryoneAsync(PublicActivityDto activity)
        => _hub.Clients.All.SendAsync("publicActivity", activity);
}
