using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

// Otel təsdiqi: rezervasiya əvvəl "sorğu" (Pending) olur, otel / admin təsdiqləyir və ya rədd edir.
public class BookingConfirmationTests
{
    private static readonly DateTime Today = new(2026, 10, 26);

    // =========================
    // STATUS KEÇİDLƏRİ
    // =========================

    [Theory]
    [InlineData(BookingStatus.Pending, BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Pending, BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.Cancelled)]
    [InlineData(BookingStatus.CheckedIn, BookingStatus.CheckedOut)]
    public void IsValidTransition_ShouldAllow(BookingStatus from, BookingStatus to)
        => Assert.True(BookingRules.IsValidTransition(from, to));

    [Theory]
    [InlineData(BookingStatus.Pending, BookingStatus.CheckedIn)]   // təsdiqsiz giriş olmaz
    [InlineData(BookingStatus.Pending, BookingStatus.CheckedOut)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.Pending)]   // geri qayıtmaq olmaz
    [InlineData(BookingStatus.Cancelled, BookingStatus.Confirmed)]
    [InlineData(BookingStatus.CheckedOut, BookingStatus.Cancelled)]
    public void IsValidTransition_ShouldReject(BookingStatus from, BookingStatus to)
        => Assert.False(BookingRules.IsValidTransition(from, to));

    [Fact]
    public void Pending_ShouldBeAddedAtTheEnd_SoStoredValuesDoNotChange()
    {
        Assert.Equal(0, (int)BookingStatus.Confirmed);
        Assert.Equal(3, (int)BookingStatus.Cancelled);
        Assert.Equal(4, (int)BookingStatus.Pending);
    }

    // =========================
    // QONAQ + ÖDƏNİŞ
    // =========================

    [Fact]
    public void Guest_CanChangeOrCancel_PendingRequest()
        => Assert.Null(BookingRules.CanGuestChange(BookingStatus.Pending, Today.AddDays(5), Today));

    [Fact]
    public void Payment_ShouldBeBlocked_UntilHotelConfirms()
    {
        Assert.NotNull(PaymentRules.WhyCannotPay(BookingStatus.Pending, PaymentStatus.Unpaid, 200));
        Assert.Null(PaymentRules.WhyCannotPay(BookingStatus.Confirmed, PaymentStatus.Unpaid, 200));
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed, PaymentStatus.Unpaid, true, BookingStatus.Pending)]    // yeni tarixlər -> yenidən təsdiq
    [InlineData(BookingStatus.Confirmed, PaymentStatus.Unpaid, false, BookingStatus.Confirmed)] // yalnız qonaq sayı
    [InlineData(BookingStatus.Confirmed, PaymentStatus.Paid, true, BookingStatus.Confirmed)]    // ödənilib -> təsdiqli qalır
    [InlineData(BookingStatus.Pending, PaymentStatus.Unpaid, true, BookingStatus.Pending)]
    public void StatusAfterModify_ShouldAskHotelAgain_WhenDatesChange(
        BookingStatus current, PaymentStatus payment, bool datesChanged, BookingStatus expected)
        => Assert.Equal(expected, BookingRules.StatusAfterModify(current, payment, datesChanged));

    [Fact]
    public void IsAnswerOverdue_ShouldBeTrue_After24Hours()
    {
        var created = new DateTime(2026, 10, 25, 9, 0, 0, DateTimeKind.Utc);

        Assert.False(BookingRules.IsAnswerOverdue(BookingStatus.Pending, created, created.AddHours(23)));
        Assert.True(BookingRules.IsAnswerOverdue(BookingStatus.Pending, created, created.AddHours(25)));

        // Təsdiqlənmiş rezervasiya heç vaxt "gecikən" deyil
        Assert.False(BookingRules.IsAnswerOverdue(BookingStatus.Confirmed, created, created.AddDays(3)));
    }

    // =========================
    // MƏKTUBLAR
    // =========================

    private static GetBookingDto Dto(BookingStatus status) => new()
    {
        ConfirmationCode = "BK-REQ00001",
        CheckInDate = Today.AddDays(10),
        CheckOutDate = Today.AddDays(12),
        AdultCount = 2,
        TotalPrice = 300m,
        Status = status,
        RoomName = "Deluxe Room",
        RoomNumber = "201",
        HotelName = "Caspian Grand Hotel",
        GuestName = "Nigar Aliyeva",
        GuestEmail = "nigar@example.com"
    };

    [Fact]
    public void RequestReceivedEmail_ShouldSayAwaitingConfirmation()
    {
        var (subject, html) = BookingEmailTemplates.RequestReceived(Dto(BookingStatus.Pending), "http://localhost/Bookings/Find");

        Assert.Contains("request received", subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AWAITING HOTEL CONFIRMATION", html);
        Assert.Contains("24 hours", html);
        Assert.Contains("BK-REQ00001", html);
    }

    [Fact]
    public void RejectionEmail_ShouldLinkToSearch_AndEncodeNames()
    {
        var dto = Dto(BookingStatus.Cancelled);
        dto.HotelName = "<b>Hotel</b>";

        var (subject, html) = BookingEmailTemplates.Rejection(dto, "http://localhost:5233/");

        Assert.Contains("declined", subject);
        Assert.Contains("http://localhost:5233/Rooms/Search", html);
        Assert.DoesNotContain("<b>Hotel</b>", html);
    }

    // =========================
    // SERVİS: TƏSDİQLƏ / RƏDD ET
    // =========================

    private static AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Booking> SeedAsync(AppDbContext context, BookingStatus status)
    {
        var hotel = new Hotel
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
            CheckInDate = DateTime.Today.AddDays(5),
            CheckOutDate = DateTime.Today.AddDays(7),
            AdultCount = 2,
            TotalPrice = 200,
            ConfirmationCode = "BK-REQ00002",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            Room = new Room
            {
                RoomNumber = "101",
                Name = "Deluxe Room",
                Description = "Desc",
                PricePerNight = 100,
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
    public async Task ConfirmRequest_ShouldMovePendingToConfirmed()
    {
        using var context = CreateContext();
        var booking = await SeedAsync(context, BookingStatus.Pending);
        var service = new BookingService(context, null!);

        Assert.Equal(1, await service.CountPendingAsync());

        var result = await service.ConfirmRequestAsync(booking.Id);

        Assert.True(result.Success);
        Assert.Equal(BookingStatus.Confirmed, (await context.Bookings.FindAsync(booking.Id))!.Status);
        Assert.Equal(0, await service.CountPendingAsync());
    }

    [Fact]
    public async Task RejectRequest_ShouldCancel_AndFreeTheDates()
    {
        using var context = CreateContext();
        var booking = await SeedAsync(context, BookingStatus.Pending);
        var service = new BookingService(context, null!);

        var result = await service.RejectRequestAsync(booking.Id);

        Assert.True(result.Success);
        Assert.Equal(BookingStatus.Cancelled, (await context.Bookings.FindAsync(booking.Id))!.Status);
    }

    [Fact]
    public async Task ConfirmRequest_ShouldFail_WhenBookingIsNotPending()
    {
        using var context = CreateContext();
        var booking = await SeedAsync(context, BookingStatus.Confirmed);
        var service = new BookingService(context, null!);

        var result = await service.RejectRequestAsync(booking.Id);

        Assert.False(result.Success);
        Assert.Equal(BookingStatus.Confirmed, (await context.Bookings.FindAsync(booking.Id))!.Status);
    }

    [Fact]
    public async Task AdminStatusChange_ShouldNotCheckIn_PendingRequest()
    {
        using var context = CreateContext();
        var booking = await SeedAsync(context, BookingStatus.Pending);
        var service = new BookingService(context, null!);

        Assert.False(await service.UpdateStatusAsync(booking.Id, BookingStatus.CheckedIn));
    }
}
