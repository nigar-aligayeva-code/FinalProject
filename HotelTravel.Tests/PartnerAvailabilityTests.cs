using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.DTOs.Notifications;
using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

// Otel partnyoru: bağlı tarixlər, qiymət, sorğunun yoxlanması, cavabsız sorğuların bağlanması
public class PartnerAvailabilityTests
{
    private static readonly DateTime Today = DateTime.Today;

    // =========================
    // QAYDALAR
    // =========================

    [Theory]
    [InlineData(0, 2, 2, 4, false)]   // bitişik: çıxış günü = giriş günü
    [InlineData(0, 3, 2, 4, true)]
    [InlineData(5, 8, 0, 6, true)]
    [InlineData(5, 8, 8, 9, false)]
    public void Overlaps_ShouldTreatEndDateAsCheckOut(int aS, int aE, int bS, int bE, bool expected)
        => Assert.Equal(expected, AvailabilityRules.Overlaps(Today.AddDays(aS), Today.AddDays(aE), Today.AddDays(bS), Today.AddDays(bE)));

    [Fact]
    public void ValidateBlock_ShouldCheckDates()
    {
        Assert.Null(AvailabilityRules.ValidateBlock(Today, Today.AddDays(3), Today));
        Assert.NotNull(AvailabilityRules.ValidateBlock(Today.AddDays(-1), Today.AddDays(3), Today));
        Assert.NotNull(AvailabilityRules.ValidateBlock(Today.AddDays(3), Today.AddDays(3), Today));
        Assert.NotNull(AvailabilityRules.ValidateBlock(Today, Today.AddDays(AvailabilityRules.MaxBlockDays + 1), Today));
    }

    [Fact]
    public void IsFresh_ShouldBeTrue_OnlyWithin24Hours()
    {
        var now = DateTime.UtcNow;

        Assert.True(AvailabilityRules.IsFresh(now.AddHours(-3), now));
        Assert.False(AvailabilityRules.IsFresh(now.AddHours(-30), now));
        Assert.False(AvailabilityRules.IsFresh(null, now));
    }

    // =========================
    // SEED
    // =========================

    private static AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(Hotel Hotel, Room Room1, Room Room2)> SeedHotelAsync(AppDbContext context)
    {
        var hotel = new Hotel
        {
            Name = "Partner Hotel",
            Description = "d",
            Address = "a",
            City = "Baku",
            Country = "Azerbaijan",
            Phone = "1",
            Email = "h@h.az",
            MainImage = "h.jpg",
            StarRating = 4
        };

        Room NewRoom(string number) => new()
        {
            RoomNumber = number,
            Name = "Deluxe " + number,
            Description = "d",
            PricePerNight = 100,
            Capacity = 2,
            MainImage = "r.jpg",
            RoomType = RoomType.Deluxe,
            Hotel = hotel
        };

        var r1 = NewRoom("101");
        var r2 = NewRoom("102");

        context.Rooms.AddRange(r1, r2);
        await context.SaveChangesAsync();

        return (hotel, r1, r2);
    }

    private static async Task<Booking> SeedBookingAsync(
        AppDbContext context, Room room, int fromDay, int toDay,
        BookingStatus status = BookingStatus.Pending, DateTime? createdAt = null, string code = "BK-PART0001")
    {
        var booking = new Booking
        {
            CheckInDate = Today.AddDays(fromDay),
            CheckOutDate = Today.AddDays(toDay),
            AdultCount = 2,
            TotalPrice = 200,
            ConfirmationCode = code,
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            Room = room,
            Guest = new Guest { FirstName = "Nigar", LastName = "A", Email = "g@test.com", Phone = "1" }
        };

        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        return booking;
    }

    // =========================
    // BAĞLI TARİXLƏR
    // =========================

    [Fact]
    public async Task AddBlock_ForAllRooms_ShouldCloseEveryRoom_AndMarkDataFresh()
    {
        using var context = CreateContext();
        var (hotel, _, _) = await SeedHotelAsync(context);
        var service = new HotelAvailabilityService(context);

        var result = await service.AddBlockAsync(hotel.Id, new CreateRoomBlockDto
        {
            StartDate = Today.AddDays(2),
            EndDate = Today.AddDays(4),
            Reason = "Sold on Booking.com"
        });

        Assert.True(result.Success);
        Assert.Equal(2, await context.RoomBlocks.CountAsync());

        var overview = await service.GetAsync(hotel.Id);
        Assert.NotNull(overview!.AvailabilityUpdatedAt);
        Assert.True(overview.IsFresh);
        Assert.Equal(2, overview.Blocks.Count);
    }

    [Fact]
    public async Task ClosedDates_ShouldHideRoomFromSearch()
    {
        using var context = CreateContext();
        var (hotel, room1, _) = await SeedHotelAsync(context);

        await new HotelAvailabilityService(context).AddBlockAsync(hotel.Id, new CreateRoomBlockDto
        {
            RoomId = room1.Id,
            StartDate = Today.AddDays(5),
            EndDate = Today.AddDays(7)
        });

        var rooms = await new RoomService(context).SearchAvailableRoomsAsync(new RoomSearchDto
        {
            CheckInDate = Today.AddDays(6),
            CheckOutDate = Today.AddDays(8),
            AdultCount = 1
        });

        // 101 bağlıdır, yalnız 102 görünür
        var only = Assert.Single(rooms);
        Assert.Equal("102", only.RoomNumber);

        // Bağlı günlərdən sonra 101 yenə görünür
        var later = await new RoomService(context).SearchAvailableRoomsAsync(new RoomSearchDto
        {
            CheckInDate = Today.AddDays(7),
            CheckOutDate = Today.AddDays(9),
            AdultCount = 1
        });

        Assert.Equal(2, later.Count);
    }

    [Fact]
    public async Task RemoveBlock_ShouldOnlyWorkForTheSameHotel()
    {
        using var context = CreateContext();
        var (hotel, room1, _) = await SeedHotelAsync(context);
        var service = new HotelAvailabilityService(context);

        await service.AddBlockAsync(hotel.Id, new CreateRoomBlockDto { RoomId = room1.Id, StartDate = Today, EndDate = Today.AddDays(1) });
        var block = await context.RoomBlocks.SingleAsync();

        Assert.False((await service.RemoveBlockAsync(hotel.Id + 99, block.Id)).Success);
        Assert.True((await service.RemoveBlockAsync(hotel.Id, block.Id)).Success);
        Assert.True((await context.RoomBlocks.SingleAsync()).IsDeleted);
    }

    [Fact]
    public async Task UpdatePrice_ShouldRejectRoomOfAnotherHotel()
    {
        using var context = CreateContext();
        var (hotel, room1, _) = await SeedHotelAsync(context);
        var service = new HotelAvailabilityService(context);

        Assert.False((await service.UpdateRoomPriceAsync(hotel.Id + 99, room1.Id, 150)).Success);
        Assert.True((await service.UpdateRoomPriceAsync(hotel.Id, room1.Id, 150)).Success);
        Assert.Equal(150m, (await context.Rooms.FindAsync(room1.Id))!.PricePerNight);
    }

    // =========================
    // SORĞUNUN YOXLANMASI
    // =========================

    [Fact]
    public async Task Check_ShouldShowClosedDates_AndOverlaps()
    {
        using var context = CreateContext();
        var (hotel, room1, _) = await SeedHotelAsync(context);
        var service = new HotelAvailabilityService(context);

        var request = await SeedBookingAsync(context, room1, 10, 12);

        var free = await service.CheckBookingAsync(request.Id);
        Assert.True(free!.LooksFree);
        Assert.False(free.HasPartner);

        await service.AddBlockAsync(hotel.Id, new CreateRoomBlockDto
        {
            RoomId = room1.Id,
            StartDate = Today.AddDays(11),
            EndDate = Today.AddDays(13),
            Reason = "Renovation"
        });

        var closed = await service.CheckBookingAsync(request.Id);
        Assert.True(closed!.IsClosedByHotel);
        Assert.Contains("Renovation", closed.ClosedReasons);
        Assert.False(closed.LooksFree);
    }

    [Fact]
    public async Task GetBookings_ShouldReturnOnlyThisHotel_PendingFirst()
    {
        using var context = CreateContext();
        var (hotel, room1, room2) = await SeedHotelAsync(context);

        await SeedBookingAsync(context, room1, 1, 2, BookingStatus.Confirmed, code: "BK-A");
        await SeedBookingAsync(context, room2, 5, 6, BookingStatus.Pending, code: "BK-B");

        var (_, otherRoom, _) = await SeedHotelAsync(context);
        await SeedBookingAsync(context, otherRoom, 1, 2, BookingStatus.Pending, code: "BK-OTHER");

        var list = await new HotelAvailabilityService(context).GetBookingsAsync(hotel.Id);

        Assert.Equal(2, list.Count);
        Assert.Equal("BK-B", list[0].ConfirmationCode);
        Assert.DoesNotContain(list, b => b.ConfirmationCode == "BK-OTHER");
    }

    // =========================
    // CAVABSIZ SORĞULAR
    // =========================

    private sealed class FakeEmails : IBookingEmailService
    {
        public List<string> Expired { get; } = new();
        public Task<(bool Success, string Message)> SendConfirmationAsync(string confirmationCode) => Task.FromResult((true, ""));
        public Task<(bool Success, string Message)> SendCancellationAsync(string confirmationCode, bool byHotel = false) => Task.FromResult((true, ""));
        public Task<(bool Success, string Message)> SendRejectionAsync(string confirmationCode) => Task.FromResult((true, ""));
        public Task<(bool Success, string Message)> SendExpiredAsync(string confirmationCode) { Expired.Add(confirmationCode); return Task.FromResult((true, "")); }
        public string GetBookingUrl(string confirmationCode) => "http://localhost";
    }

    private sealed class FakeNotifier : IActivityNotifier
    {
        public List<(string Code, string Kind)> Guest { get; } = new();
        public Task HotelBookedAsync(string confirmationCode) => Task.CompletedTask;
        public Task HotelCancelledAsync(string confirmationCode) => Task.CompletedTask;
        public Task FlightBookedAsync(string confirmationCode) => Task.CompletedTask;
        public Task TaxiBookedAsync(string confirmationCode) => Task.CompletedTask;
        public Task CarRentedAsync(string confirmationCode) => Task.CompletedTask;
        public Task TripBookedAsync(string packageCode) => Task.CompletedTask;
        public Task GuestBookingUpdateAsync(string confirmationCode, string kind) { Guest.Add((confirmationCode, kind)); return Task.CompletedTask; }
        public Task PartnerNewRequestAsync(string confirmationCode) => Task.CompletedTask;
        public Task GuestFlightCancelledAsync(int flightBookingId) => Task.CompletedTask;
        public Task ToGuestAsync(string? appUserId, GuestNotificationDto notification) => Task.CompletedTask;
    }

    [Fact]
    public async Task Expiry_ShouldCloseOnlyRequestsOlderThan24Hours()
    {
        using var context = CreateContext();
        var (_, room1, room2) = await SeedHotelAsync(context);
        var now = DateTime.UtcNow;

        var old = await SeedBookingAsync(context, room1, 3, 4, BookingStatus.Pending, now.AddHours(-30), "BK-OLD");
        var fresh = await SeedBookingAsync(context, room2, 3, 4, BookingStatus.Pending, now.AddHours(-2), "BK-NEW");
        var confirmed = await SeedBookingAsync(context, room2, 8, 9, BookingStatus.Confirmed, now.AddDays(-5), "BK-CONF");

        var emails = new FakeEmails();
        var notifier = new FakeNotifier();

        var count = await new RequestExpiryService(context, emails, notifier).ExpireOverdueAsync(now);

        Assert.Equal(1, count);
        Assert.Equal(BookingStatus.Cancelled, (await context.Bookings.FindAsync(old.Id))!.Status);
        Assert.Equal(BookingStatus.Pending, (await context.Bookings.FindAsync(fresh.Id))!.Status);
        Assert.Equal(BookingStatus.Confirmed, (await context.Bookings.FindAsync(confirmed.Id))!.Status);
        Assert.Equal(new[] { "BK-OLD" }, emails.Expired);
        Assert.Contains(("BK-OLD", "expired"), notifier.Guest);
    }
}
