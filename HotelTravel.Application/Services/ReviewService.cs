using HotelTravel.Application.DTOs.Reviews;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IAppDbContext _context;

    public ReviewService(IAppDbContext context)
    {
        _context = context;
    }


    // =========================
    // CHECK ELIGIBILITY
    // =========================
    public async Task<ReviewEligibilityDto> CheckEligibilityAsync(
        string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
        {
            return new ReviewEligibilityDto
            {
                CanReview = false,
                Message = "Confirmation code is required."
            };
        }

        confirmationCode = confirmationCode.Trim().ToUpper();

        var booking = await _context.Bookings
            .Where(b =>
                !b.IsDeleted &&
                b.ConfirmationCode.ToUpper() == confirmationCode)
            .Select(b => new
            {
                b.ConfirmationCode,
                b.Status,
                b.RoomId,
                RoomName = b.Room.Name,
                RoomNumber = b.Room.RoomNumber,
                HotelName = b.Room.Hotel.Name,
                b.CheckInDate,
                b.CheckOutDate,
                HasReview = b.Review != null
            })
            .FirstOrDefaultAsync();

        if (booking is null)
        {
            return new ReviewEligibilityDto
            {
                CanReview = false,
                Message = "Booking was not found."
            };
        }

        var result = new ReviewEligibilityDto
        {
            ConfirmationCode = booking.ConfirmationCode,
            RoomId = booking.RoomId,
            RoomName = booking.RoomName,
            RoomNumber = booking.RoomNumber,
            HotelName = booking.HotelName,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate
        };

        if (booking.Status != BookingStatus.CheckedOut)
        {
            result.CanReview = false;
            result.Message =
                "You can leave a review only after your stay is completed (checked out).";

            return result;
        }

        if (booking.HasReview)
        {
            result.CanReview = false;
            result.Message =
                "A review has already been submitted for this booking.";

            return result;
        }

        result.CanReview = true;
        return result;
    }


    // =========================
    // CREATE REVIEW
    // =========================
    public async Task<(bool Success, string Message, int? RoomId)>
        CreateAsync(CreateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            return (false, "Rating must be between 1 and 5.", null);

        if (string.IsNullOrWhiteSpace(dto.Comment))
            return (false, "Please write a comment.", null);

        if (dto.HotelRating is < 1 or > 5)
            return (false, "Rating must be between 1 and 5.", null);

        if (string.IsNullOrWhiteSpace(dto.ConfirmationCode) ||
            string.IsNullOrWhiteSpace(dto.Email))
        {
            return (false, "Confirmation code and email are required.", null);
        }

        var confirmationCode = dto.ConfirmationCode.Trim().ToUpper();
        var email = dto.Email.Trim().ToLower();

        // 1. Rezervasiya + email uyğunluğu
        var booking = await _context.Bookings
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b =>
                !b.IsDeleted &&
                b.ConfirmationCode.ToUpper() == confirmationCode &&
                b.Guest.Email.ToLower() == email);

        if (booking is null)
        {
            return (
                false,
                "The email address does not match this booking.",
                null);
        }

        // 2. Yalnız tamamlanmış qalış
        if (booking.Status != BookingStatus.CheckedOut)
        {
            return (
                false,
                "You can leave a review only after your stay is completed (checked out).",
                null);
        }

        // 3. Hər rezervasiyaya bir rəy
        if (booking.Review != null)
        {
            return (
                false,
                "A review has already been submitted for this booking.",
                null);
        }

        var review = new Review
        {
            Rating = dto.Rating,
            Comment = dto.Comment.Trim(),
            BookingId = booking.Id,
            RoomId = booking.RoomId,

            // Otel rəyi istəyə bağlıdır: ulduz verilməyibsə şərh də saxlanmır
            HotelRating = dto.HotelRating,
            HotelComment = dto.HotelRating is null || string.IsNullOrWhiteSpace(dto.HotelComment)
                ? null
                : dto.HotelComment.Trim(),

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _context.Reviews.AddAsync(review);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Eyni anda iki dəfə göndərilibsə,
            // unique index ikincisini dayandırır
            return (
                false,
                "A review has already been submitted for this booking.",
                null);
        }

        return (
            true,
            "Thank you! Your review has been published.",
            booking.RoomId);
    }


    // =========================
    // ROOM REVIEWS
    // =========================
    public async Task<List<GetReviewDto>> GetByRoomIdAsync(int roomId)
    {
        return await ProjectReviews(
                _context.Reviews.Where(r =>
                    !r.IsDeleted &&
                    r.RoomId == roomId))
            .ToListAsync();
    }


    // =========================
    // ROOM RATING SUMMARY
    // =========================
    public async Task<RoomRatingSummaryDto> GetRoomSummaryAsync(int roomId)
    {
        var ratings = await _context.Reviews
            .Where(r =>
                !r.IsDeleted &&
                r.RoomId == roomId)
            .Select(r => r.Rating)
            .ToListAsync();

        var summary = new RoomRatingSummaryDto
        {
            RoomId = roomId,
            ReviewCount = ratings.Count,
            AverageRating = ratings.Count == 0
                ? 0
                : Math.Round(ratings.Average(), 1)
        };

        for (int star = 5; star >= 1; star--)
        {
            summary.StarCounts[star] =
                ratings.Count(r => r == star);
        }

        return summary;
    }


    // =========================
    // HOTEL REVIEWS (otelə qiymət verilmiş rəylər)
    // =========================
    public async Task<List<GetReviewDto>> GetByHotelIdAsync(int hotelId)
    {
        return await ProjectReviews(
                _context.Reviews.Where(r =>
                    !r.IsDeleted &&
                    r.Room.HotelId == hotelId &&
                    r.HotelRating != null))
            .ToListAsync();
    }


    // =========================
    // HOTEL RATING SUMMARY
    // =========================
    public async Task<HotelRatingSummaryDto> GetHotelSummaryAsync(int hotelId)
    {
        var reviews = await _context.Reviews
            .Where(r =>
                !r.IsDeleted &&
                r.Room.HotelId == hotelId)
            .Select(r => new { r.Rating, r.HotelRating })
            .ToListAsync();

        var hotelRatings = reviews
            .Where(r => r.HotelRating != null)
            .Select(r => r.HotelRating!.Value)
            .ToList();

        var summary = new HotelRatingSummaryDto
        {
            HotelId = hotelId,
            ReviewCount = hotelRatings.Count,
            AverageRating = hotelRatings.Count == 0 ? 0 : Math.Round(hotelRatings.Average(), 1),
            RoomReviewCount = reviews.Count,
            RoomAverageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.Rating), 1)
        };

        for (int star = 5; star >= 1; star--)
        {
            summary.StarCounts[star] = hotelRatings.Count(r => r == star);
        }

        return summary;
    }


    // =========================
    // ADMIN - ALL REVIEWS
    // =========================
    public async Task<List<GetReviewDto>> GetAllAsync()
    {
        return await ProjectReviews(
                _context.Reviews.Where(r => !r.IsDeleted))
            .ToListAsync();
    }


    // =========================
    // ADMIN - DELETE (SOFT)
    // =========================
    public async Task<bool> DeleteAsync(int id)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                !r.IsDeleted);

        if (review is null)
            return false;

        review.IsDeleted = true;
        review.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }


    // =========================
    // PRIVATE HELPER
    // =========================
    private static IQueryable<GetReviewDto> ProjectReviews(
        IQueryable<Review> query)
    {
        return query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new GetReviewDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,

                GuestDisplayName =
                    r.Booking.Guest.FirstName + " " +
                    r.Booking.Guest.LastName.Substring(0, 1) + ".",

                RoomId = r.RoomId,
                RoomName = r.Room.Name,
                RoomNumber = r.Room.RoomNumber,

                BookingId = r.BookingId,
                ConfirmationCode = r.Booking.ConfirmationCode,

                CheckInDate = r.Booking.CheckInDate,
                CheckOutDate = r.Booking.CheckOutDate,

                HotelId = r.Room.HotelId,
                HotelName = r.Room.Hotel.Name,
                HotelRating = r.HotelRating,
                HotelComment = r.HotelComment
            });
    }
}
