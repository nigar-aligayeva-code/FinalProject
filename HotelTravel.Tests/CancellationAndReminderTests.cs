using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.DTOs.Notifications;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Options;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

// Ləğv cəriməsi (CancellationPolicy) və xatırlatmalar (ReminderRules / ReminderService)
public class CancellationAndReminderTests
{
    // 26 Oktyabr 2026, saat 10:00
    private static readonly DateTime Now = new(2026, 10, 26, 10, 0, 0);

    // =========================
    // OTEL LƏĞVİ
    // =========================

    [Fact]
    public void Hotel_ShouldBeFree_ThreeDaysOrMoreBefore()
    {
        // Giriş 30 Okt 14:00 -> 100 saat qalıb
        var q = CancellationPolicy.ForHotel(BookingStatus.Confirmed, Now.Date.AddDays(4), 300m, Now);

        Assert.True(q.Allowed);
        Assert.True(q.IsFree);
    }

    [Fact]
    public void Hotel_ShouldCharge30Percent_BetweenOneAndThreeDays()
    {
        // Giriş 28 Okt 14:00 -> 52 saat qalıb
        var q = CancellationPolicy.ForHotel(BookingStatus.Confirmed, Now.Date.AddDays(2), 300m, Now);

        Assert.True(q.Allowed);
        Assert.Equal(30, q.FeePercent);
        Assert.Equal(90m, q.Fee);
    }

    [Fact]
    public void Hotel_ShouldCharge50Percent_InTheLast24Hours()
    {
        // Giriş sabah 14:00, indi 20:00 -> 18 saat qalıb
        var evening = Now.Date.AddHours(20);
        var q = CancellationPolicy.ForHotel(BookingStatus.Confirmed, Now.Date.AddDays(1), 300m, evening);

        Assert.Equal(50, q.FeePercent);
        Assert.Equal(150m, q.Fee);
    }

    [Fact]
    public void Hotel_ShouldNotCancel_OnCheckInDay()
        => Assert.False(CancellationPolicy.ForHotel(BookingStatus.Confirmed, Now.Date, 300m, Now).Allowed);

    [Fact]
    public void Hotel_PendingRequest_IsAlwaysFree()
    {
        var q = CancellationPolicy.ForHotel(BookingStatus.Pending, Now.Date.AddDays(1), 300m, Now);

        Assert.True(q.IsFree);
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.CheckedOut)]
    public void Hotel_ShouldNotCancel_InOtherStatuses(BookingStatus status)
        => Assert.False(CancellationPolicy.ForHotel(status, Now.Date.AddDays(10), 300m, Now).Allowed);

    // =========================
    // UÇUŞ LƏĞVİ
    // =========================

    [Theory]
    [InlineData(100, true, 0)]
    [InlineData(48, true, 30)]
    [InlineData(10, true, 50)]
    [InlineData(2, false, 0)]   // son 3 saat - olmur
    public void Flight_FeeDependsOnHoursLeft(int hoursLeft, bool allowed, int percent)
    {
        var q = CancellationPolicy.ForFlight(FlightBookingStatus.Confirmed, Now.AddHours(hoursLeft), 200m, Now);

        Assert.Equal(allowed, q.Allowed);
        Assert.Equal(percent, q.FeePercent);
    }

    [Fact]
    public void FeeFor_ShouldRoundToQapik()
        => Assert.Equal(33.34m, CancellationPolicy.FeeFor(111.13m, 30));

    // =========================
    // SERVİS: QONAQ LƏĞVİ CƏRİMƏNİ SAXLAYIR
    // =========================

    private static AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Booking> SeedBookingAsync(
        AppDbContext context,
        DateTime checkIn,
        BookingStatus status = BookingStatus.Confirmed,
        string code = "BK-CAN00001",
        string? userId = "user-1")
    {
        var hotel = await context.Hotels.FirstOrDefaultAsync() ?? new Hotel
        {
            Name = "Test Hotel",
            Description = "Desc",
            Address = "Address",
            City = "Baku",
            Country = "Azerbaijan",
            Phone = "000",
            Email = "hotel@test.com",
            MainImage = "hotel.jpg",
            StarRating = 4
        };

        var booking = new Booking
        {
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2),
            AdultCount = 2,
            TotalPrice = 300,
            OriginalPrice = 300,
            ConfirmationCode = code,
            Status = status,
            AppUserId = userId,
            Room = new Room
            {
                RoomNumber = "101",
                Name = "Deluxe Room",
                Description = "Desc",
                PricePerNight = 150,
                Capacity = 2,
                MainImage = "room.jpg",
                RoomType = RoomType.Deluxe,
                Hotel = hotel
            },
            Guest = new Guest
            {
                FirstName = "Nigar",
                LastName = "Aliyeva",
                Email = "guest@test.com",
                Phone = "+994000000000"
            }
        };

        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        return booking;
    }

    [Fact]
    public async Task CancelByGuest_ShouldStoreFee_WhenLate()
    {
        using var context = CreateContext();

        // Sabaha giriş -> həmişə 30% və ya 50% cərimə
        var booking = await SeedBookingAsync(context, DateTime.Today.AddDays(1));
        var service = new BookingService(context, null!);

        var result = await service.CancelByGuestAsync(booking.ConfirmationCode, "guest@test.com");

        Assert.True(result.Success);

        var saved = await context.Bookings.SingleAsync();
        Assert.Equal(BookingStatus.Cancelled, saved.Status);
        Assert.True(saved.CancellationFee > 0);
        Assert.NotNull(saved.CancelledAt);
    }

    [Fact]
    public async Task CancelByGuest_ShouldBeFree_WhenFarAway()
    {
        using var context = CreateContext();
        var booking = await SeedBookingAsync(context, DateTime.Today.AddDays(10));
        var service = new BookingService(context, null!);

        await service.CancelByGuestAsync(booking.ConfirmationCode, "guest@test.com");

        Assert.Equal(0m, (await context.Bookings.SingleAsync()).CancellationFee);
    }

    [Fact]
    public async Task AdminCancel_ShouldNotChargeTheGuest()
    {
        using var context = CreateContext();
        var booking = await SeedBookingAsync(context, DateTime.Today.AddDays(1));
        var service = new BookingService(context, null!);

        Assert.True(await service.UpdateStatusAsync(booking.Id, BookingStatus.Cancelled));
        Assert.Equal(0m, (await context.Bookings.SingleAsync()).CancellationFee);
    }

    // =========================
    // XATIRLATMA QAYDALARI
    // =========================

    [Theory]
    [InlineData(1, ReminderKind.HotelTomorrow)]
    [InlineData(2, ReminderKind.HotelUpcoming)]
    [InlineData(3, ReminderKind.HotelUpcoming)]
    [InlineData(4, ReminderKind.None)]
    [InlineData(0, ReminderKind.None)]
    public void HotelReminder_DependsOnDaysLeft(int days, ReminderKind expected)
        => Assert.Equal(expected, ReminderRules.ForHotel(Now.Date.AddDays(days), Now));

    [Theory]
    [InlineData(30, ReminderKind.None)]
    [InlineData(20, ReminderKind.FlightDayBefore)]
    [InlineData(2, ReminderKind.FlightSoon)]
    [InlineData(-1, ReminderKind.None)]
    public void FlightReminder_DependsOnHoursLeft(int hours, ReminderKind expected)
        => Assert.Equal(expected, ReminderRules.ForFlight(Now.AddHours(hours), Now));

    // =========================
    // XATIRLATMA SERVİSİ
    // =========================

    // Məktubu EmailLog-a yazır (real EmailOutbox kimi) - təkrar göndərilməməsini yoxlamaq üçün
    private sealed class FakeOutbox : IEmailOutbox
    {
        private readonly AppDbContext _context;
        public List<EmailMessage> Sent { get; } = new();

        public FakeOutbox(AppDbContext context) => _context = context;

        public async Task<(bool Success, string Message)> SendAsync(EmailMessage message, string type, string? referenceCode)
        {
            Sent.Add(message);
            _context.EmailLogs.Add(new EmailLog { To = message.To, Subject = message.Subject, Type = type, ReferenceCode = referenceCode, Delivered = true });
            await _context.SaveChangesAsync();
            return (true, "sent");
        }
    }

    private sealed class FakeNotifier : IActivityNotifier
    {
        public List<(string? UserId, GuestNotificationDto Dto)> Guest { get; } = new();

        public Task HotelBookedAsync(string confirmationCode) => Task.CompletedTask;
        public Task HotelCancelledAsync(string confirmationCode) => Task.CompletedTask;
        public Task FlightBookedAsync(string confirmationCode) => Task.CompletedTask;
        public Task TaxiBookedAsync(string confirmationCode) => Task.CompletedTask;
        public Task CarRentedAsync(string confirmationCode) => Task.CompletedTask;
        public Task TripBookedAsync(string packageCode) => Task.CompletedTask;
        public Task GuestBookingUpdateAsync(string confirmationCode, string kind) => Task.CompletedTask;
        public Task GuestFlightCancelledAsync(int flightBookingId) => Task.CompletedTask;
        public Task PartnerNewRequestAsync(string confirmationCode) => Task.CompletedTask;

        public Task ToGuestAsync(string? appUserId, GuestNotificationDto notification)
        {
            Guest.Add((appUserId, notification));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBookingEmails : IBookingEmailService
    {
        public Task<(bool Success, string Message)> SendConfirmationAsync(string confirmationCode) => Task.FromResult((true, ""));
        public Task<(bool Success, string Message)> SendCancellationAsync(string confirmationCode, bool byHotel = false) => Task.FromResult((true, ""));
        public Task<(bool Success, string Message)> SendRejectionAsync(string confirmationCode) => Task.FromResult((true, ""));
        public Task<(bool Success, string Message)> SendExpiredAsync(string confirmationCode) => Task.FromResult((true, ""));
        public string GetBookingUrl(string confirmationCode) => "http://localhost/Bookings/Find?confirmationCode=" + confirmationCode;
    }

    private static (ReminderService Service, FakeOutbox Outbox, FakeNotifier Notifier) CreateReminders(AppDbContext context)
    {
        var outbox = new FakeOutbox(context);
        var notifier = new FakeNotifier();

        var service = new ReminderService(
            context,
            new BookingService(context, null!),
            new FakeBookingEmails(),
            outbox,
            notifier,
            new EmailOptions());

        return (service, outbox, notifier);
    }

    [Fact]
    public async Task Reminders_ShouldSendOnce_ForConfirmedStayTomorrow()
    {
        using var context = CreateContext();
        var now = DateTime.Today.AddHours(10);
        await SeedBookingAsync(context, DateTime.Today.AddDays(1));

        var (service, outbox, notifier) = CreateReminders(context);

        Assert.Equal(1, await service.SendDueAsync(now));
        Assert.Contains("tomorrow", outbox.Sent[0].Subject);
        Assert.Equal("user-1", notifier.Guest[0].UserId);
        Assert.Equal("hotel_tomorrow", notifier.Guest[0].Dto.Kind);

        // İkinci dəfə: artıq göndərilib
        Assert.Equal(0, await service.SendDueAsync(now.AddMinutes(10)));
    }

    [Fact]
    public async Task Reminders_ShouldSkip_PendingAndCancelledBookings()
    {
        using var context = CreateContext();
        await SeedBookingAsync(context, DateTime.Today.AddDays(1), BookingStatus.Pending, "BK-CAN00002");
        await SeedBookingAsync(context, DateTime.Today.AddDays(2), BookingStatus.Cancelled, "BK-CAN00003");

        var (service, _, _) = CreateReminders(context);

        Assert.Equal(0, await service.SendDueAsync(DateTime.Today.AddHours(10)));
    }

    [Fact]
    public async Task Reminders_ShouldSendFlightReminders_24hAnd3h()
    {
        using var context = CreateContext();
        var now = DateTime.Today.AddHours(8);

        context.FlightBookings.Add(new FlightBooking
        {
            ConfirmationCode = "FL-TEST0001",
            FlightNumber = "J2 101",
            Airline = "AZAL",
            FromCity = "Baku",
            FromCode = "GYD",
            ToCity = "Istanbul",
            ToCode = "IST",
            DepartureTime = now.AddHours(20),
            ArrivalTime = now.AddHours(23),
            FlightDate = now.Date,
            Passengers = 1,
            TotalPrice = 300,
            Status = FlightBookingStatus.Confirmed,
            ContactName = "Nigar Aliyeva",
            Email = "guest@test.com",
            Phone = "1",
            AppUserId = "user-1",
            FlightRoute = new FlightRoute
            {
                FromCity = "Baku",
                FromCode = "GYD",
                ToCity = "Istanbul",
                ToCode = "IST",
                FlightNumber = "J2 101",
                Airline = "AZAL"
            }
        });
        await context.SaveChangesAsync();

        var (service, outbox, _) = CreateReminders(context);

        // 20 saat qalıb -> "24 saat" xatırlatması
        Assert.Equal(1, await service.SendDueAsync(now));

        // 2 saat qalıb -> "3 saat" xatırlatması (ayrı tip)
        Assert.Equal(1, await service.SendDueAsync(now.AddHours(18)));
        Assert.Contains("few hours", outbox.Sent[1].Subject);

        // Hər ikisi göndərilib -> daha yox
        Assert.Equal(0, await service.SendDueAsync(now.AddHours(18.5)));
    }
}
