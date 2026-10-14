using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class PricingCalculatorTests
{
    private static readonly DateTime Today = new(2026, 10, 14);
    private const string Email = "guest@test.com";

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Room NewRoom(string city, decimal price) => new()
    {
        RoomNumber = "101",
        Name = "Room " + city,
        Description = "Desc",
        PricePerNight = price,
        Capacity = 2,
        MainImage = "room.jpg",
        RoomType = RoomType.Double,
        Hotel = new Hotel
        {
            Name = "Hotel " + city,
            Description = "Desc",
            Address = "Address",
            City = city,
            Country = "Country",
            Phone = "000",
            Email = "hotel@test.com",
            MainImage = "hotel.jpg",
            StarRating = 5
        }
    };

    private static void AddPastStay(AppDbContext context, Room room, BookingStatus status)
    {
        context.Bookings.Add(new Booking
        {
            Room = room,
            CheckInDate = Today.AddDays(-20),
            CheckOutDate = Today.AddDays(-18),
            AdultCount = 1,
            TotalPrice = 100,
            ConfirmationCode = "BK-" + Guid.NewGuid().ToString("N")[..8],
            Status = status,
            Guest = new Guest
            {
                FirstName = "Nigar",
                LastName = "A",
                Email = Email,
                Phone = "000"
            }
        });
    }


    [Fact]
    public async Task Quote_ShouldMultiplyNights_WithoutDiscount()
    {
        using var context = CreateContext();
        var room = NewRoom("Batumi", 150);
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var quote = await PricingCalculator.QuoteAsync(
            context, room.Id, Today.AddDays(1), Today.AddDays(4),
            Email, null, null, Today);

        Assert.True(quote.Success);
        Assert.Equal(3, quote.Nights);
        Assert.Equal(450m, quote.BaseTotal);
        Assert.Equal(450m, quote.Total);
        Assert.Equal(0, quote.DiscountPercent);
    }

    [Fact]
    public async Task Quote_ShouldGiveExplorerReward_ForStayInAnotherCity()
    {
        using var context = CreateContext();
        var baku = NewRoom("Baku", 100);
        var batumi = NewRoom("Batumi", 200);
        context.Rooms.AddRange(baku, batumi);
        AddPastStay(context, baku, BookingStatus.CheckedOut);
        await context.SaveChangesAsync();

        var quote = await PricingCalculator.QuoteAsync(
            context, batumi.Id, Today.AddDays(1), Today.AddDays(3),
            "GUEST@test.com", null, null, Today);

        Assert.True(quote.LoyaltyEligible);
        Assert.Equal("Baku", quote.LoyaltyCity);
        Assert.Equal(BookingRules.ExplorerRewardPercent, quote.DiscountPercent);
        Assert.Equal(360m, quote.Total);
    }

    [Fact]
    public async Task Quote_ShouldNotGiveExplorerReward_ForSameCityOrNotCompleted()
    {
        using var context = CreateContext();
        var baku = NewRoom("Baku", 100);
        var batumi = NewRoom("Batumi", 200);
        context.Rooms.AddRange(baku, batumi);

        AddPastStay(context, batumi, BookingStatus.CheckedOut); // eyni şəhər
        AddPastStay(context, baku, BookingStatus.Cancelled);    // tamamlanmayıb
        await context.SaveChangesAsync();

        var quote = await PricingCalculator.QuoteAsync(
            context, batumi.Id, Today.AddDays(1), Today.AddDays(3),
            Email, null, null, Today);

        Assert.False(quote.LoyaltyEligible);
        Assert.Equal(400m, quote.Total);
    }

    [Fact]
    public async Task Quote_ShouldApplyValidPromo_AndReportInvalidOne()
    {
        using var context = CreateContext();
        var room = NewRoom("Baku", 100);
        context.Rooms.Add(room);
        context.PromoCodes.AddRange(
            new PromoCode { Code = "SAVE20", DiscountPercent = 20, IsActive = true },
            new PromoCode { Code = "OLD", DiscountPercent = 30, IsActive = true, ValidUntil = Today.AddDays(-1) });
        await context.SaveChangesAsync();

        var valid = await PricingCalculator.QuoteAsync(
            context, room.Id, Today.AddDays(1), Today.AddDays(3),
            Email, null, " save20 ", Today);

        var expired = await PricingCalculator.QuoteAsync(
            context, room.Id, Today.AddDays(1), Today.AddDays(3),
            Email, null, "OLD", Today);

        Assert.Equal("SAVE20", valid.AppliedPromoCode);
        Assert.Equal(160m, valid.Total);

        Assert.NotNull(expired.PromoError);
        Assert.Null(expired.AppliedPromoCode);
        Assert.Equal(200m, expired.Total);
    }

    [Fact]
    public async Task Quote_ShouldUseBiggerDiscount_NotStack()
    {
        using var context = CreateContext();
        var baku = NewRoom("Baku", 100);
        var batumi = NewRoom("Batumi", 100);
        context.Rooms.AddRange(baku, batumi);
        AddPastStay(context, baku, BookingStatus.CheckedOut);      // 10%
        context.PromoCodes.Add(new PromoCode { Code = "SMALL5", DiscountPercent = 5, IsActive = true });
        await context.SaveChangesAsync();

        var quote = await PricingCalculator.QuoteAsync(
            context, batumi.Id, Today.AddDays(1), Today.AddDays(2),
            Email, null, "SMALL5", Today);

        Assert.Equal(10, quote.DiscountPercent);
        Assert.Null(quote.AppliedPromoCode);
        Assert.Equal(90m, quote.Total);
    }
}
