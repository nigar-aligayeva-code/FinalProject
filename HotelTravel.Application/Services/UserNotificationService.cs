using HotelTravel.Application.DTOs.Notifications;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

/// <summary>
/// Qonağın bildirişləri: bazaya yazılır (saytda olmayanda da itmir),
/// qonaq həmin an saytdadırsa SignalR ilə dərhal 🔔-ə düşür.
/// </summary>
public class UserNotificationService : IUserNotificationService
{
    // Hər istifadəçi üçün ən çox bu qədər bildiriş saxlanılır
    public const int KeepPerUser = 50;

    private readonly IAppDbContext _context;
    private readonly IRealtimePublisher _publisher;

    public UserNotificationService(IAppDbContext context, IRealtimePublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<GuestNotificationDto?> AddAsync(string appUserId, GuestNotificationDto notification)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return null;

        var entity = new UserNotification
        {
            AppUserId = appUserId,
            Kind = Cut(notification.Kind, 40),
            Place = Cut(notification.Place, 200),
            Code = Cut(notification.Code, 50),
            When = notification.When,
            Url = notification.Url,
            Message = Cut(notification.Message, 500),
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserNotifications.Add(entity);
        await _context.SaveChangesAsync();

        await TrimAsync(appUserId);

        var dto = ToDto(entity);

        // Canlı göndərmə alınmasa da bildiriş bazada qalır
        try
        {
            await _publisher.ToUserAsync(appUserId, dto);
        }
        catch (Exception)
        {
        }

        return dto;
    }

    public async Task<List<GuestNotificationDto>> GetMineAsync(string appUserId, int take = 20)
    {
        var rows = await _context.UserNotifications
            .Where(n => n.AppUserId == appUserId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(Math.Clamp(take, 1, KeepPerUser))
            .ToListAsync();

        return rows.Select(ToDto).ToList();
    }

    public Task<int> CountUnreadAsync(string appUserId)
        => _context.UserNotifications.CountAsync(n =>
            n.AppUserId == appUserId && !n.IsDeleted && !n.IsRead);

    public async Task MarkAllReadAsync(string appUserId)
    {
        var unread = await _context.UserNotifications
            .Where(n => n.AppUserId == appUserId && !n.IsDeleted && !n.IsRead)
            .ToListAsync();

        if (unread.Count == 0)
            return;

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    // Köhnələri sil ki, cədvəl sonsuz böyüməsin
    private async Task TrimAsync(string appUserId)
    {
        var old = await _context.UserNotifications
            .Where(n => n.AppUserId == appUserId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip(KeepPerUser)
            .ToListAsync();

        if (old.Count == 0)
            return;

        _context.UserNotifications.RemoveRange(old);
        await _context.SaveChangesAsync();
    }

    private static GuestNotificationDto ToDto(UserNotification n) => new()
    {
        Id = n.Id,
        Kind = n.Kind,
        Place = n.Place,
        Code = n.Code,
        When = n.When,
        Url = n.Url,
        Message = n.Message,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };

    private static string Cut(string? text, int max)
    {
        text ??= string.Empty;
        return text.Length <= max ? text : text[..max];
    }
}
