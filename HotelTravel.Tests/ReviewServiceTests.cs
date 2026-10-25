using HotelTravel.Application.DTOs.Reviews;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class ReviewServiceTests
{
    private const string GuestEmail = "guest@test.com";

    // Hər test üçün ayrıca in-memory baza
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<Booking> SeedBookingAsync(
        AppDbContext context,
        BookingStatus status,
        string code = "BK-TEST0001")
    {
        var hotel = await context.Hotels.FirstOrDefaultAsync();

        if (hotel is null)
        {
            hotel = new Hotel
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
            };

            context.Hotels.Add(hotel);
        }

        var room = await context.Rooms.FirstOrDefaultAsync();

        if (room is null)
        {
            room = new Room
            {
                RoomNumber = "101",
                Name = "Deluxe Room",
                Description = "Desc",
                PricePerNight = 100,
                Capacity = 2,
                MainImage = "room.jpg",
                RoomType = RoomType.Deluxe,
                Hotel = hotel
            };

            context.Rooms.Add(room);
        }

        var booking = new Booking
        {
            CheckInDate = DateTime.Today.AddDays(-3),
            CheckOutDate = DateTime.Today.AddDays(-1),
            AdultCount = 2,
            ChildrenCount = 0,
            TotalPrice = 200,
            ConfirmationCode = code,
            Status = status,
            Room = room,
            Guest = new Guest
            {
                FirstName = "Nigar",
                LastName = "Aliyeva",
                Email = GuestEmail,
                Phone = "+994000000000"
            }
        };

        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        return booking;
    }

    private static CreateReviewDto ValidDto(
        string code = "BK-TEST0001",
        int rating = 5) => new()
    {
        ConfirmationCode = code,
        Email = GuestEmail,
        Rating = rating,
        Comment = "Very clean room and friendly staff."
    };


    [Fact]
    public async Task Create_ShouldSucceed_WhenStayIsCheckedOut()
    {
        using var context = CreateContext();
        var booking = await SeedBookingAsync(context, BookingStatus.CheckedOut);
        var service = new ReviewService(context);

        var result = await service.CreateAsync(ValidDto());

        Assert.True(result.Success);
        Assert.Equal(booking.RoomId, result.RoomId);
        Assert.Single(context.Reviews);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task Create_ShouldFail_WhenStayIsNotCompleted(
        BookingStatus status)
    {
        using var context = CreateContext();
        await SeedBookingAsync(context, status);
        var service = new ReviewService(context);

        var result = await service.CreateAsync(ValidDto());

        Assert.False(result.Success);
        Assert.Empty(context.Reviews);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenEmailDoesNotMatch()
    {
        using var context = CreateContext();
        await SeedBookingAsync(context, BookingStatus.CheckedOut);
        var service = new ReviewService(context);

        var dto = ValidDto();
        dto.Email = "someone.else@test.com";

        var result = await service.CreateAsync(dto);

        Assert.False(result.Success);
        Assert.Empty(context.Reviews);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenBookingAlreadyReviewed()
    {
        using var context = CreateContext();
        await SeedBookingAsync(context, BookingStatus.CheckedOut);
        var service = new ReviewService(context);

        var first = await service.CreateAsync(ValidDto());
        var second = await service.CreateAsync(ValidDto(rating: 1));

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Single(context.Reviews);
    }

    [Fact]
    public async Task Create_ShouldBeCaseInsensitive_ForCodeAndEmail()
    {
        using var context = CreateContext();
        await SeedBookingAsync(context, BookingStatus.CheckedOut);
        var service = new ReviewService(context);

        var dto = ValidDto(code: " bk-test0001 ");
        dto.Email = "GUEST@TEST.COM";

        var result = await service.CreateAsync(dto);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Summary_ShouldCalculateAverage_AndIgnoreDeleted()
    {
        using var context = CreateContext();
        var b1 = await SeedBookingAsync(context, BookingStatus.CheckedOut, "BK-A");
        await SeedBookingAsync(context, BookingStatus.CheckedOut, "BK-B");
        await SeedBookingAsync(context, BookingStatus.CheckedOut, "BK-C");
        var service = new ReviewService(context);

        await service.CreateAsync(ValidDto("BK-A", 5));
        await service.CreateAsync(ValidDto("BK-B", 4));
        await service.CreateAsync(ValidDto("BK-C", 1));

        // Admin 1 ulduzlu rəyi silir
        var lowReview = await context.Reviews.FirstAsync(r => r.Rating == 1);
        await service.DeleteAsync(lowReview.Id);

        var summary = await service.GetRoomSummaryAsync(b1.RoomId);

        Assert.Equal(2, summary.ReviewCount);
        Assert.Equal(4.5, summary.AverageRating);
        Assert.Equal(1, summary.StarCounts[5]);
        Assert.Equal(1, summary.StarCounts[4]);
        Assert.Equal(0, summary.StarCounts[1]);
    }

    [Fact]
    public async Task CheckEligibility_ShouldAllow_OnlyCheckedOutWithoutReview()
    {
        using var context = CreateContext();
        await SeedBookingAsync(context, BookingStatus.CheckedOut);
        var service = new ReviewService(context);

        var before = await service.CheckEligibilityAsync("BK-TEST0001");
        await service.CreateAsync(ValidDto());
        var after = await service.CheckEligibilityAsync("BK-TEST0001");

        Assert.True(before.CanReview);
        Assert.False(after.CanReview);
    }


    // =========================
    // OTEL RƏYLƏRİ
    // =========================

    [Fact]
    public async Task Create_ShouldSaveHotelRating_WhenGiven()
    {
        using var context = CreateContext();
        await SeedBookingAsync(context, BookingStatus.CheckedOut);
        var service = new ReviewService(context);

        var dto = ValidDto();
        dto.HotelRating = 4;
        dto.HotelComment = "  Great breakfast and location.  ";

        var result = await service.CreateAsync(dto);

        Assert.True(result.Success);
        var review = await context.Reviews.SingleAsync();
        Assert.Equal(4, review.HotelRating);
        Assert.Equal("Great breakfast and location.", review.HotelComment);
    }

    [Fact]
    public async Task Create_ShouldIgnoreHotelComment_WithoutHotelRating()
    {
        using var context = CreateContext();
        await SeedBookingAsync(context, BookingStatus.CheckedOut);
        var service = new ReviewService(context);

        var dto = ValidDto();
        dto.HotelComment = "No stars given";

        await service.CreateAsync(dto);

        var review = await context.Reviews.SingleAsync();
        Assert.Null(review.HotelRating);
        Assert.Null(review.HotelComment);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenHotelRatingIsOutOfRange()
    {
        using var context = CreateContext();
        await SeedBookingAsync(context, BookingStatus.CheckedOut);
        var service = new ReviewService(context);

        var dto = ValidDto();
        dto.HotelRating = 7;

        var result = await service.CreateAsync(dto);

        Assert.False(result.Success);
        Assert.Empty(context.Reviews);
    }

    [Fact]
    public async Task HotelSummary_ShouldAverageOnlyHotelRatings()
    {
        using var context = CreateContext();
        await SeedBookingAsync(context, BookingStatus.CheckedOut, "BK-A");
        await SeedBookingAsync(context, BookingStatus.CheckedOut, "BK-B");
        var service = new ReviewService(context);

        var first = ValidDto("BK-A", rating: 5);
        first.HotelRating = 4;
        await service.CreateAsync(first);

        // İkinci qonaq otelə qiymət vermədi
        await service.CreateAsync(ValidDto("BK-B", rating: 3));

        var hotelId = (await context.Hotels.SingleAsync()).Id;
        var summary = await service.GetHotelSummaryAsync(hotelId);
        var hotelReviews = await service.GetByHotelIdAsync(hotelId);

        Assert.Equal(1, summary.ReviewCount);
        Assert.Equal(4, summary.AverageRating);
        Assert.Equal(2, summary.RoomReviewCount);
        Assert.Equal(4, summary.RoomAverageRating);
        Assert.Single(hotelReviews);
        Assert.Equal("Test Hotel", hotelReviews[0].HotelName);
    }
}
