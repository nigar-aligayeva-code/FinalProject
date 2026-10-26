using HotelTravel.Application.DTOs.Notifications;

namespace HotelTravel.Application.Interfaces;

// Qonağın 🔔 bildirişləri (bazada saxlanılır)
public interface IUserNotificationService
{
    // Saxla və qonaq saytdadırsa canlı göndər
    Task<GuestNotificationDto?> AddAsync(string appUserId, GuestNotificationDto notification);

    Task<List<GuestNotificationDto>> GetMineAsync(string appUserId, int take = 20);

    Task<int> CountUnreadAsync(string appUserId);

    Task MarkAllReadAsync(string appUserId);
}
