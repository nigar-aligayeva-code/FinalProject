using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using HotelTravel.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class RoomVarietySeederTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Hotel NewHotel(string name) => new()
    {
        Name = name, Description = "d", Address = "a", City = "c", Country = "c",
        Phone = "1", Email = "h@h.az", MainImage = "", StarRating = 5,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    private static Room BaseRoom(Hotel hotel, string suffix, RoomType type, decimal price) => new()
    {
        HotelId = hotel.Id, RoomNumber = $"{hotel.Id}{suffix}", Name = type.ToString(), Description = "d",
        PricePerNight = price, Capacity = 2, RoomType = type, MainImage = "",
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    private static async Task<Hotel> SeedHotelWithBaseRoomsAsync(AppDbContext context, string name)
    {
        var hotel = NewHotel(name);
        context.Hotels.Add(hotel);
        await context.SaveChangesAsync();

        context.Rooms.AddRange(
            BaseRoom(hotel, "01", RoomType.Single, 90m),
            BaseRoom(hotel, "02", RoomType.Double, 140m),
            BaseRoom(hotel, "03", RoomType.Deluxe, 220m),
            BaseRoom(hotel, "04", RoomType.Suite, 350m),
            BaseRoom(hotel, "05", RoomType.Family, 280m));
        await context.SaveChangesAsync();

        return hotel;
    }

    [Fact]
    public void Hotels_ShouldHaveDifferentRoomTypes()
    {
        var dubai = RoomVarietySeeder.RoomTypesFor("Dubai Marina Luxury Hotel");
        var tbilisi = RoomVarietySeeder.RoomTypesFor("Tbilisi Heritage Hotel");
        var gabala = RoomVarietySeeder.RoomTypesFor("Gabala Mountain Resort");

        Assert.Contains(RoomType.Penthouse, dubai);
        Assert.DoesNotContain(RoomType.Penthouse, tbilisi);
        Assert.Contains(RoomType.Villa, gabala);
        Assert.DoesNotContain(RoomType.Suite, gabala);
        Assert.Contains(RoomType.King, RoomVarietySeeder.RoomTypesFor("Caspian Grand Hotel"));
        Assert.DoesNotContain(RoomType.King, tbilisi);
    }

    [Fact]
    public void EveryHotel_ShouldOfferAtLeastSixRooms_AndFourTypes()
    {
        foreach (var name in new[] { "Caspian Grand Hotel", "Gabala Mountain Resort", "Bosphorus Palace Hotel",
                     "Antalya Blue Resort", "Tbilisi Heritage Hotel", "Batumi Sea View Hotel", "Dubai Marina Luxury Hotel", "HotelHub Baku" })
        {
            var profile = RoomVarietySeeder.ProfileFor(name)!;

            Assert.True(profile.BaseRooms.Length + profile.Signature.Length >= 6, name);
            Assert.True(RoomVarietySeeder.RoomTypesFor(name).Count >= 4, name);
        }
    }

    [Fact]
    public void UnknownHotel_ShouldNotBeTouched()
    {
        Assert.Null(RoomVarietySeeder.ProfileFor("My New Hotel"));
    }

    [Fact]
    public async Task Seed_ShouldHideUnbookedRooms_AddSignatureRooms_AndAdjustPrices()
    {
        await using var context = CreateContext();
        var hotel = await SeedHotelWithBaseRoomsAsync(context, "Dubai Marina Luxury Hotel");

        await RoomVarietySeeder.SeedAsync(context);

        var active = await context.Rooms.Where(r => r.HotelId == hotel.Id && !r.IsDeleted).ToListAsync();

        // Dubay: yalnız Deluxe (03) və Suite (04) qalır + 6 imza otaq
        Assert.DoesNotContain(active, r => r.RoomType == RoomType.Single);
        Assert.Contains(active, r => r.RoomType == RoomType.Penthouse);
        Assert.Equal(8, active.Count);

        // Qiymət 1.6 dəfə: 220 -> 350
        Assert.Equal(350m, active.Single(r => r.RoomType == RoomType.Deluxe).PricePerNight);
    }

    [Fact]
    public async Task Seed_ShouldKeepRoomsThatHaveBookings()
    {
        await using var context = CreateContext();
        var hotel = await SeedHotelWithBaseRoomsAsync(context, "Dubai Marina Luxury Hotel");
        var single = await context.Rooms.SingleAsync(r => r.RoomType == RoomType.Single);

        context.Bookings.Add(new Booking { RoomId = single.Id, ConfirmationCode = "BK-TEST", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        await RoomVarietySeeder.SeedAsync(context);

        Assert.False((await context.Rooms.FindAsync(single.Id))!.IsDeleted);
    }

    [Fact]
    public async Task Seed_ShouldBeIdempotent()
    {
        await using var context = CreateContext();
        await SeedHotelWithBaseRoomsAsync(context, "Batumi Sea View Hotel");

        await RoomVarietySeeder.SeedAsync(context);
        var first = await context.Rooms.CountAsync();

        await RoomVarietySeeder.SeedAsync(context);

        Assert.Equal(first, await context.Rooms.CountAsync());
    }
}
