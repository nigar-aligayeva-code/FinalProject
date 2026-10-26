using HotelTravel.Application.DTOs.Payments;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Options;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class PaymentTests
{
    // Stripe əvəzinə saxta gateway: şəbəkə yoxdur, nəticəni test idarə edir
    private sealed class FakeGateway : IPaymentGateway
    {
        public bool IsEnabled { get; init; } = true;
        public PaymentCheckoutRequest? LastRequest { get; private set; }
        public PaymentSessionStatus Status { get; set; } = new(true, "BK-PAY1", 24000, "azn");

        public Task<PaymentCheckoutSession> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            return Task.FromResult(new PaymentCheckoutSession("cs_test_123", "https://checkout.stripe.com/pay/cs_test_123"));
        }

        public Task<PaymentSessionStatus> GetSessionAsync(string sessionId, CancellationToken ct = default)
            => Task.FromResult(Status);

        public long? LastRefundMinor { get; private set; }
        public bool RefundSucceeds { get; set; } = true;

        public Task<PaymentRefundResult> RefundAsync(string sessionId, long amountMinor, CancellationToken ct = default)
        {
            LastRefundMinor = amountMinor;
            return Task.FromResult(new PaymentRefundResult(RefundSucceeds, RefundSucceeds ? "re_test_1" : null, RefundSucceeds ? null : "declined"));
        }
    }

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task SeedAsync(AppDbContext context, BookingStatus status = BookingStatus.Confirmed)
    {
        var hotel = new Hotel { Name = "Test Hotel", Description = "d", Address = "a", City = "Baku", Country = "AZ", Phone = "1", Email = "h@h.az", MainImage = "", StarRating = 5 };
        var room = new Room { RoomNumber = "101", Name = "Deluxe", Description = "d", PricePerNight = 120, Capacity = 2, MainImage = "", RoomType = RoomType.Deluxe, Hotel = hotel };

        context.Bookings.Add(new Booking
        {
            ConfirmationCode = "BK-PAY1",
            CheckInDate = DateTime.Today.AddDays(5),
            CheckOutDate = DateTime.Today.AddDays(7),
            AdultCount = 2,
            TotalPrice = 240m,
            Status = status,
            Room = room,
            Guest = new Guest { FirstName = "Nigar", LastName = "A", Email = "nigar@gmail.com", Phone = "1" }
        });

        await context.SaveChangesAsync();
    }

    [Theory]
    [InlineData(123.45, 12345)]
    [InlineData(240, 24000)]
    [InlineData(0.005, 1)]
    public void ToMinorUnits_ShouldConvertToQapik(decimal amount, long expected)
    {
        Assert.Equal(expected, PaymentRules.ToMinorUnits(amount));
    }

    [Fact]
    public void WhyCannotPay_ShouldBlockPaidCancelledAndCompleted()
    {
        Assert.NotNull(PaymentRules.WhyCannotPay(BookingStatus.Confirmed, PaymentStatus.Paid, 100));
        Assert.NotNull(PaymentRules.WhyCannotPay(BookingStatus.Cancelled, PaymentStatus.Unpaid, 100));
        Assert.NotNull(PaymentRules.WhyCannotPay(BookingStatus.CheckedOut, PaymentStatus.Unpaid, 100));
        Assert.Null(PaymentRules.WhyCannotPay(BookingStatus.Confirmed, PaymentStatus.Unpaid, 100));
    }

    [Fact]
    public void StripeOptions_ShouldOnlyTrustTestKeys()
    {
        Assert.True(new StripeOptions { SecretKey = "sk_test_abc" }.IsTestKey);
        Assert.False(new StripeOptions { SecretKey = "sk_live_abc" }.IsTestKey);
        Assert.False(new StripeOptions().IsEnabled);
    }

    [Fact]
    public async Task Start_ShouldCreateCheckout_WithAmountInQapik()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var gateway = new FakeGateway();
        var service = new PaymentService(context, gateway, new StripeOptions { SecretKey = "sk_test_x" });

        var result = await service.StartAsync(new StartPaymentDto { ConfirmationCode = "bk-pay1", SuccessUrl = "http://s", CancelUrl = "http://c" });

        Assert.True(result.Success);
        Assert.Equal(24000, gateway.LastRequest!.AmountMinor);
        Assert.Equal("cs_test_123", (await context.Bookings.SingleAsync()).PaymentReference);
    }

    [Fact]
    public async Task Confirm_ShouldMarkPaid_OnlyWhenStripeSaysPaid()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var gateway = new FakeGateway();
        var service = new PaymentService(context, gateway, new StripeOptions { SecretKey = "sk_test_x" });
        await service.StartAsync(new StartPaymentDto { ConfirmationCode = "BK-PAY1", SuccessUrl = "s", CancelUrl = "c" });

        // Stripe "ödənilməyib" deyir -> Paid olmamalıdır
        gateway.Status = new PaymentSessionStatus(false, "BK-PAY1", 24000, "azn");
        var failed = await service.ConfirmAsync(new ConfirmPaymentDto { SessionId = "cs_test_123" });
        Assert.False(failed.Success);
        Assert.Equal(PaymentStatus.Unpaid, (await context.Bookings.SingleAsync()).PaymentStatus);

        // Məbləğ uyğun deyil -> yenə Paid deyil
        gateway.Status = new PaymentSessionStatus(true, "BK-PAY1", 100, "azn");
        Assert.False((await service.ConfirmAsync(new ConfirmPaymentDto { SessionId = "cs_test_123" })).Success);

        // Hər şey uyğundur -> Paid
        gateway.Status = new PaymentSessionStatus(true, "BK-PAY1", 24000, "azn");
        var ok = await service.ConfirmAsync(new ConfirmPaymentDto { SessionId = "cs_test_123" });

        Assert.True(ok.Success);
        var booking = await context.Bookings.SingleAsync();
        Assert.Equal(PaymentStatus.Paid, booking.PaymentStatus);
        Assert.NotNull(booking.PaidAt);
    }

    [Fact]
    public async Task Start_ShouldFail_WhenPaymentsDisabled()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = new PaymentService(context, new FakeGateway { IsEnabled = false }, new StripeOptions());

        var result = await service.StartAsync(new StartPaymentDto { ConfirmationCode = "BK-PAY1" });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Refund_ShouldReturnPaidAmountMinusFee()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        // Ödənilib, sonra 30% cərimə ilə ləğv edilib
        var booking = await context.Bookings.SingleAsync();
        booking.PaymentStatus = PaymentStatus.Paid;
        booking.PaymentReference = "cs_test_123";
        booking.Status = BookingStatus.Cancelled;
        booking.CancellationFee = 72m;
        await context.SaveChangesAsync();

        var gateway = new FakeGateway();
        var service = new PaymentService(context, gateway, new StripeOptions { SecretKey = "sk_test_x" });

        var result = await service.RefundCancelledAsync("BK-PAY1");

        Assert.True(result.Success);
        Assert.Equal(16800, gateway.LastRefundMinor);   // 240 - 72 = 168 AZN

        booking = await context.Bookings.SingleAsync();
        Assert.Equal(PaymentStatus.Refunded, booking.PaymentStatus);
        Assert.Equal(168m, booking.RefundAmount);
    }

    [Fact]
    public async Task Refund_ShouldDoNothing_ForUnpaidBooking()
    {
        await using var context = CreateContext();
        await SeedAsync(context, BookingStatus.Cancelled);

        var gateway = new FakeGateway();
        var service = new PaymentService(context, gateway, new StripeOptions { SecretKey = "sk_test_x" });

        var result = await service.RefundCancelledAsync("BK-PAY1");

        Assert.False(result.Success);
        Assert.Null(gateway.LastRefundMinor);
    }

    [Fact]
    public async Task Refund_ShouldKeepPaid_WhenStripeFails()
    {
        await using var context = CreateContext();
        await SeedAsync(context, BookingStatus.Cancelled);

        var booking = await context.Bookings.SingleAsync();
        booking.PaymentStatus = PaymentStatus.Paid;
        booking.PaymentReference = "cs_test_123";
        await context.SaveChangesAsync();

        var service = new PaymentService(context, new FakeGateway { RefundSucceeds = false }, new StripeOptions { SecretKey = "sk_test_x" });

        var result = await service.RefundCancelledAsync("BK-PAY1");

        Assert.False(result.Success);
        Assert.Equal(PaymentStatus.Paid, (await context.Bookings.SingleAsync()).PaymentStatus);
    }

    [Theory]
    [InlineData(240, 72, 168)]
    [InlineData(240, 0, 240)]
    [InlineData(100, 150, 0)]
    public void RefundFor_ShouldSubtractFee_AndNeverGoNegative(decimal paid, decimal fee, decimal expected)
        => Assert.Equal(expected, PaymentRules.RefundFor(paid, fee));
}
