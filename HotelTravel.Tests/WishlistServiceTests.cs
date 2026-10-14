using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class WishlistServiceTests
{
    private const string UserId = "user-1";

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<Room> SeedRoomAsync(
        AppDbContext context,
        bool deleted = false)
    {
        var room = new Room
        {
            RoomNumber = "101",
            Name = "Deluxe Room",
            Description = "Desc",
            PricePerNight = 100,
            Capacity = 2,
            MainImage = "room.jpg",
            RoomType = RoomType.Deluxe,
            IsDeleted = deleted,
            Hotel = new Hotel
            {
                Name = "Test Hotel",
                Description = "Desc",
                Address = "Address",
                City = "Baku",
                Country = "Azerbaijan",
                Phone = "000",
                Email = "hotel@test.com",
                MainImage = "hotel.jpg",
                StarRating = 5
            }
        };

        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        return room;
    }


    [Fact]
    public async Task Toggle_ShouldSave_ThenRemove()
    {
        using var context = CreateContext();
        var room = await SeedRoomAsync(context);
        var service = new WishlistService(context);

        var first = await service.ToggleAsync(UserId, room.Id);
        var second = await service.ToggleAsync(UserId, room.Id);

        Assert.True(first.Success);
        Assert.True(first.Saved);

        Assert.True(second.Success);
        Assert.False(second.Saved);

        Assert.Empty(context.WishlistItems);
    }

    [Fact]
    public async Task Toggle_ShouldFail_ForDeletedRoom()
    {
        using var context = CreateContext();
        var room = await SeedRoomAsync(context, deleted: true);
        var service = new WishlistService(context);

        var result = await service.ToggleAsync(UserId, room.Id);

        Assert.False(result.Success);
        Assert.Empty(context.WishlistItems);
    }

    [Fact]
    public async Task SavedRoomIds_ShouldBeEmpty_ForAnonymousUser()
    {
        using var context = CreateContext();
        var service = new WishlistService(context);

        var ids = await service.GetSavedRoomIdsAsync(null);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task SavedRooms_ShouldOnlyReturnCurrentUsersRooms()
    {
        using var context = CreateContext();
        var room = await SeedRoomAsync(context);
        var service = new WishlistService(context);

        await service.ToggleAsync(UserId, room.Id);
        await service.ToggleAsync("other-user", room.Id);

        var mine = await service.GetSavedRoomsAsync(UserId);
        var ids = await service.GetSavedRoomIdsAsync(UserId);

        Assert.Single(mine);
        Assert.Equal(room.Id, mine[0].Id);
        Assert.Contains(room.Id, ids);
    }
}
