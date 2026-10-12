
using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
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
    CreateAsync(CreateBookingDto dto)
    {
        try
        {
            return await ExecuteWithRetryAsync(
                () => CreateOnceAsync(dto),
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
     CreateOnceAsync(CreateBookingDto dto)
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
                r.Id == dto.RoomId && !r.IsDeleted);

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

        var numberOfNights =
            (dto.CheckOutDate.Date - dto.CheckInDate.Date).Days;

        var totalPrice = numberOfNights * room.PricePerNight;

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
            TotalPrice = totalPrice,
            ConfirmationCode = confirmationCode,
            Status = BookingStatus.Confirmed,
            RoomId = room.Id,
            Guest = guest,
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
}
