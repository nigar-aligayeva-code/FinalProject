
using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("HotelTravel.Tests")]
namespace HotelTravel.Infrastructure.Services;

public class BookingCreationService : IBookingCreationService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public BookingCreationService(
        IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(bool Success, string Message, string? ConfirmationCode)>
    CreateAsync(CreateBookingDto dto, string? appUserId = null)
    {
        try
        {
            return await ExecuteWithRetryAsync(
                () => CreateOnceAsync(dto, appUserId),
                SqlServerErrorHelper.IsDeadlock);
        }
        catch (Exception ex) when (SqlServerErrorHelper.IsDeadlock(ex))
        {
            return (
                false,
                "Booking is temporarily unavailable. Please try again.",
                null);
        }
    }
    internal static async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation,
    Func<Exception, bool> isDeadlock)
    {
        const int maxAttempts = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (isDeadlock(ex))
            {
                if (attempt == maxAttempts)
                {
                    throw;
                }

                await Task.Delay(100 * attempt);
            }
        }

        throw new InvalidOperationException(
            "Retry attempts exhausted.");
    }
    private async Task<(bool Success, string Message, string? ConfirmationCode)>
     CreateOnceAsync(CreateBookingDto dto, string? appUserId)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        if (dto.CheckInDate.Date < DateTime.Today)
            return (false, "Check-in date cannot be in the past.", null);

        if (dto.CheckInDate.Date >= dto.CheckOutDate.Date)
            return (false, "Check-out date must be after check-in date.", null);

        await using var transaction =
     await ((IAppDbContext)context).BeginTransactionAsync();

        var room = await context.Rooms
            .FirstOrDefaultAsync(r =>
                r.Id == dto.RoomId &&
                !r.IsDeleted &&
                !r.Hotel.IsDeleted);

        if (room is null)
            return (false, "Room not found.", null);

        var totalGuests = dto.AdultCount + dto.ChildrenCount;

        if (dto.AdultCount <= 0 ||
            dto.ChildrenCount < 0 ||
            totalGuests > room.Capacity)
        {
            return (false, "Guest count is not valid for this room.", null);
        }

        var roomIsBooked = await context.Bookings
            .AnyAsync(b =>
                b.RoomId == dto.RoomId &&
                !b.IsDeleted &&
                b.Status != BookingStatus.Cancelled &&
                dto.CheckInDate.Date < b.CheckOutDate.Date &&
                dto.CheckOutDate.Date > b.CheckInDate.Date);

        if (roomIsBooked)
        {
            return (
                false,
                "This room is not available for the selected dates.",
                null);
        }

        // F6 + endirimlər (promo kod / Explorer reward)
        var quote = await PricingCalculator.QuoteAsync(
            context,
            room.Id,
            dto.CheckInDate,
            dto.CheckOutDate,
            dto.Email,
            appUserId,
            dto.PromoCode,
            DateTime.Today);

        if (!quote.Success)
            return (false, quote.Error ?? "Price could not be calculated.", null);

        // Qonaq kod yazıbsa, amma kod yanlışdırsa - xəbər verək
        if (!string.IsNullOrWhiteSpace(dto.PromoCode) &&
            quote.PromoError != null)
        {
            return (false, quote.PromoError, null);
        }

        // Promo kod tətbiq olundusa, istifadə sayını artır
        if (quote.AppliedPromoCode != null)
        {
            var promo = await context.PromoCodes
                .FirstAsync(p => p.Code == quote.AppliedPromoCode);

            promo.UsedCount++;
            promo.UpdatedAt = DateTime.UtcNow;
        }

        var guest = new Guest
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await context.Guests.AddAsync(guest);

        var confirmationCode =
            $"BK-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var booking = new Booking
        {
            CheckInDate = dto.CheckInDate.Date,
            CheckOutDate = dto.CheckOutDate.Date,
            AdultCount = dto.AdultCount,
            ChildrenCount = dto.ChildrenCount,
            TotalPrice = quote.Total,
            OriginalPrice = quote.BaseTotal,
            DiscountPercent = quote.DiscountPercent,
            DiscountAmount = quote.DiscountAmount,
            DiscountNote = quote.DiscountNote,
            PromoCode = quote.AppliedPromoCode,
            ConfirmationCode = confirmationCode,
            Status = BookingStatus.Confirmed,
            RoomId = room.Id,
            Guest = guest,
            AppUserId = appUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await context.Bookings.AddAsync(booking);

        await context.SaveChangesAsync();

        await transaction.CommitAsync();

        return (
            true,
            "Booking created successfully.",
            confirmationCode);
    }


    // =========================
    // F4 - MODIFY BOOKING
    // =========================
    public async Task<(bool Success, string Message)>
        ModifyAsync(ModifyBookingDto dto, string? appUserId = null)
    {
        try
        {
            return await ExecuteWithRetryAsync(
                () => ModifyOnceAsync(dto, appUserId),
                SqlServerErrorHelper.IsDeadlock);
        }
        catch (Exception ex) when (SqlServerErrorHelper.IsDeadlock(ex))
        {
            return (
                false,
                "Booking is temporarily unavailable. Please try again.");
        }
    }

    private async Task<(bool Success, string Message)>
        ModifyOnceAsync(ModifyBookingDto dto, string? appUserId)
    {
        if (string.IsNullOrWhiteSpace(dto.ConfirmationCode))
            return (false, "Confirmation code is required.");

        var confirmationCode = dto.ConfirmationCode.Trim().ToUpper();

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        // Başqa rezervasiya eyni anda bu otağı tuta bilməsin
        await using var transaction =
            await ((IAppDbContext)context).BeginTransactionAsync();

        var booking = await context.Bookings
            .Include(b => b.Room)
            .Include(b => b.Guest)
            .FirstOrDefaultAsync(b =>
                !b.IsDeleted &&
                b.ConfirmationCode.ToUpper() == confirmationCode);

        if (booking is null)
            return (false, "Booking was not found.");

        // 1. Sahiblik: daxil olmuş sahib və ya düzgün email
        var isOwnerUser =
            !string.IsNullOrEmpty(appUserId) &&
            booking.AppUserId == appUserId;

        var emailMatches =
            !string.IsNullOrWhiteSpace(dto.Email) &&
            string.Equals(
                booking.Guest.Email?.Trim(),
                dto.Email.Trim(),
                StringComparison.OrdinalIgnoreCase);

        if (!isOwnerUser && !emailMatches)
            return (false, "The email address does not match this booking.");

        var today = DateTime.Today;

        // 2. Status və vaxt
        var changeError = BookingRules.CanGuestChange(
            booking.Status,
            booking.CheckInDate,
            today);

        if (changeError != null)
            return (false, changeError);

        // 3. Yeni tarix və qonaq sayı
        var stayError = BookingRules.ValidateStay(
            dto.CheckInDate,
            dto.CheckOutDate,
            dto.AdultCount,
            dto.ChildrenCount,
            booking.Room.Capacity,
            today);

        if (stayError != null)
            return (false, stayError);

        var newCheckIn = dto.CheckInDate.Date;
        var newCheckOut = dto.CheckOutDate.Date;

        // 4. Heç nə dəyişməyibsə
        if (booking.CheckInDate.Date == newCheckIn &&
            booking.CheckOutDate.Date == newCheckOut &&
            booking.AdultCount == dto.AdultCount &&
            booking.ChildrenCount == dto.ChildrenCount)
        {
            return (false, "No changes were made.");
        }

        // 5. Əlçatanlıq (bu rezervasiyanın özü nəzərə alınmır)
        var roomIsBooked = await context.Bookings
            .AnyAsync(b =>
                b.Id != booking.Id &&
                b.RoomId == booking.RoomId &&
                !b.IsDeleted &&
                b.Status != BookingStatus.Cancelled &&
                newCheckIn < b.CheckOutDate.Date &&
                newCheckOut > b.CheckInDate.Date);

        if (roomIsBooked)
        {
            return (
                false,
                "This room is not available for the selected dates.");
        }

        // 6. Yenilə və qiyməti yenidən hesabla
        booking.CheckInDate = newCheckIn;
        booking.CheckOutDate = newCheckOut;
        booking.AdultCount = dto.AdultCount;
        booking.ChildrenCount = dto.ChildrenCount;
        // Yeni qiymət: əvvəlki endirim faizi saxlanılır
        var newBase = BookingRules.CalculateTotal(
            booking.Room.PricePerNight,
            newCheckIn,
            newCheckOut);

        var (newDiscount, newTotal) = BookingRules.ApplyDiscount(
            newBase,
            booking.DiscountPercent);

        booking.OriginalPrice = newBase;
        booking.DiscountAmount = newDiscount;
        booking.TotalPrice = newTotal;
        booking.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (
            true,
            $"Booking updated successfully. New total: {booking.TotalPrice:0.00} AZN.");
    }
}
