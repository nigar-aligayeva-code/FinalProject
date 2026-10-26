using HotelTravel.Application.DTOs.Notifications;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

// Qonağın 🔔 bildirişləri: bazada saxlanır, canlı göndərilir, oxundu kimi işarələnir
public class UserNotificationTests
{
    private sealed class FakePublisher : IRealtimePublisher
    {
        public List<(string UserId, GuestNotificationDto Dto)> Sent { get; } = new();

        public Task ToAdminsAsync(LiveNotificationDto notification) => Task.CompletedTask;
        public Task ToEveryoneAsync(PublicActivityDto activity) => Task.CompletedTask;

        public Task ToUserAsync(string userId, GuestNotificationDto notification)
        {
            Sent.Add((userId, notification));
            return Task.CompletedTask;
        }
    }

    private static AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static GuestNotificationDto Note(string kind = "confirmed", string code = "BK-N1") => new()
    {
        Kind = kind,
        Place = "Caspian Grand Hotel",
        Code = code,
        When = "30 Oct",
        Url = "/Bookings/MyBookings",
        Message = "Caspian Grand Hotel confirmed your booking."
    };

    [Fact]
    public async Task Add_ShouldSave_AndPushLiveToThatUserOnly()
    {
        using var context = CreateContext();
        var publisher = new FakePublisher();
        var service = new UserNotificationService(context, publisher);

        var saved = await service.AddAsync("user-1", Note());

        Assert.NotNull(saved);
        Assert.True(saved!.Id > 0);
        Assert.Single(publisher.Sent);
        Assert.Equal("user-1", publisher.Sent[0].UserId);
        Assert.Equal(1, await service.CountUnreadAsync("user-1"));
        Assert.Equal(0, await service.CountUnreadAsync("user-2"));
    }

    [Fact]
    public async Task GetMine_ShouldReturnNewestFirst()
    {
        using var context = CreateContext();
        var service = new UserNotificationService(context, new FakePublisher());

        await service.AddAsync("user-1", Note("requested", "BK-A"));
        await service.AddAsync("user-1", Note("confirmed", "BK-B"));

        var mine = await service.GetMineAsync("user-1");

        Assert.Equal(2, mine.Count);
        Assert.Equal("BK-B", mine[0].Code);
    }

    [Fact]
    public async Task MarkAllRead_ShouldClearTheBadge()
    {
        using var context = CreateContext();
        var service = new UserNotificationService(context, new FakePublisher());

        await service.AddAsync("user-1", Note());
        await service.AddAsync("user-1", Note("hotel_reminder"));

        await service.MarkAllReadAsync("user-1");

        Assert.Equal(0, await service.CountUnreadAsync("user-1"));
        Assert.All(await service.GetMineAsync("user-1"), n => Assert.True(n.IsRead));
    }

    [Fact]
    public async Task Add_ShouldKeepOnlyTheLatest50()
    {
        using var context = CreateContext();
        var service = new UserNotificationService(context, new FakePublisher());

        for (var i = 0; i < UserNotificationService.KeepPerUser + 5; i++)
            await service.AddAsync("user-1", Note(code: $"BK-{i}"));

        Assert.Equal(UserNotificationService.KeepPerUser, await context.UserNotifications.CountAsync());
    }

    [Fact]
    public async Task Add_ShouldIgnore_GuestWithoutAccount()
    {
        using var context = CreateContext();
        var publisher = new FakePublisher();
        var service = new UserNotificationService(context, publisher);

        Assert.Null(await service.AddAsync("", Note()));
        Assert.Empty(publisher.Sent);
    }
}
