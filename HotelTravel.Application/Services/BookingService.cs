using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class BookingService : IBookingService
{

    private readonly IAppDbContext _context;
    private readonly IBookingCreationService _bookingCreationService;

    public BookingService(
        IAppDbContext context,
        IBookingCreationService bookingCreationService)
    {
        _context = context;
        _bookingCreationService = bookingCreationService;
    }


    public async Task<List<GetBookingDto>> GetAllAsync()
    {
        return await _context.Bookings
            .Where(b => !b.IsDeleted)
            .Select(b => new GetBookingDto
            {
                Id = b.Id,

                ConfirmationCode = b.ConfirmationCode,

                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,

                AdultCount = b.AdultCount,
                ChildrenCount = b.ChildrenCount,

                TotalPrice = b.TotalPrice,

                Status = b.Status,

                RoomId = b.RoomId,
                RoomName = b.Room.Name,
                RoomNumber = b.Room.RoomNumber,

                GuestId = b.GuestId,
                GuestName = b.Guest.FirstName + " " + b.Guest.LastName,
                GuestEmail = b.Guest.Email
            })
            .ToListAsync();
    }

    public async Task<GetBookingDto?> GetByIdAsync(int id)
    {
        return await _context.Bookings
            .Where(b => b.Id == id && !b.IsDeleted)
            .Select(b => new GetBookingDto
            {
                Id = b.Id,

                ConfirmationCode = b.ConfirmationCode,

                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,

                AdultCount = b.AdultCount,
                ChildrenCount = b.ChildrenCount,

                TotalPrice = b.TotalPrice,

                Status = b.Status,

                RoomId = b.RoomId,
                RoomName = b.Room.Name,
                RoomNumber = b.Room.RoomNumber,

                GuestId = b.GuestId,
                GuestName = b.Guest.FirstName + " " + b.Guest.LastName,
                GuestEmail = b.Guest.Email
            })
            .FirstOrDefaultAsync();
    }

   

public async Task<(bool Success, string Message, string? ConfirmationCode)>


    CreateAsync(CreateBookingDto dto)
    {
        return await _bookingCreationService.CreateAsync(dto);
    }

    public async Task<bool> UpdateStatusAsync(
        int id,
        BookingStatus status)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                !b.IsDeleted);

        if (booking is null)
            return false;

        // 1. Status keçidi düzgündür?
        bool isValidTransition =
            (booking.Status == BookingStatus.Confirmed &&
                (status == BookingStatus.CheckedIn ||
                 status == BookingStatus.Cancelled))
            ||
            (booking.Status == BookingStatus.CheckedIn &&
                status == BookingStatus.CheckedOut);

        if (!isValidTransition)
            return false;

        // 2. Tarix yoxlamaları
        var today = DateTime.Today;

        // Giriş tarixindən əvvəl CheckedIn etmək olmaz
        if (status == BookingStatus.CheckedIn &&
            today < booking.CheckInDate.Date)
        {
            return false;
        }

        // CheckedOut giriş tarixindən əvvəl edilə bilməz
        if (status == BookingStatus.CheckedOut &&
            today < booking.CheckInDate.Date)
        {
            return false;
        }

        // 3. Statusu dəyiş
        booking.Status = status;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<OccupancyCalendarDto>>
    GetOccupancyCalendarAsync()
    {
        return await _context.Bookings
            .Where(b =>
                !b.IsDeleted &&
                b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.CheckInDate)
            .Select(b => new OccupancyCalendarDto
            {
                BookingId = b.Id,

                RoomId = b.RoomId,

                RoomNumber = b.Room.RoomNumber,

                RoomName = b.Room.Name,

                CheckInDate = b.CheckInDate,

                CheckOutDate = b.CheckOutDate,

                GuestName =
                    b.Guest.FirstName + " " + b.Guest.LastName,

                ConfirmationCode = b.ConfirmationCode,

                Status = b.Status.ToString()
            })
            .ToListAsync();
    }
    public async Task<GetBookingDto?> GetByConfirmationCodeAsync(
    string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return null;

        confirmationCode = confirmationCode.Trim().ToUpper();

        return await _context.Bookings
            .Where(b =>
                !b.IsDeleted &&
                b.ConfirmationCode.ToUpper() == confirmationCode)
            .Select(b => new GetBookingDto
            {
                Id = b.Id,

                ConfirmationCode = b.ConfirmationCode,

                CheckInDate = b.CheckInDate,

                CheckOutDate = b.CheckOutDate,

                AdultCount = b.AdultCount,

                ChildrenCount = b.ChildrenCount,

                TotalPrice = b.TotalPrice,

                Status = b.Status,

                RoomId = b.RoomId,

                RoomName = b.Room.Name,

                RoomNumber = b.Room.RoomNumber,

                GuestId = b.GuestId,

                GuestName =
                    b.Guest.FirstName + " " + b.Guest.LastName,

                GuestEmail = b.Guest.Email
            })
            .FirstOrDefaultAsync();
    }
    public async Task<(bool Success, string Message)>
    CancelByGuestAsync(
        string confirmationCode,
        string email)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode) ||
            string.IsNullOrWhiteSpace(email))
        {
            return (
                false,
                "Confirmation code and email are required."
            );
        }

        confirmationCode =
            confirmationCode.Trim().ToUpper();

        email =
            email.Trim().ToLower();

        var booking =
            await _context.Bookings
                .FirstOrDefaultAsync(b =>
                    !b.IsDeleted &&
                    b.ConfirmationCode.ToUpper() ==
                        confirmationCode &&
                    b.Guest.Email.ToLower() ==
                        email);

        if (booking is null)
        {
            return (
                false,
                "The email address is incorrect."
            );
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return (
                false,
                "This booking is already cancelled."
            );
        }

        if (booking.Status == BookingStatus.CheckedIn ||
            booking.Status == BookingStatus.CheckedOut)
        {
            return (
                false,
                "This booking can no longer be cancelled."
            );
        }

        if (booking.CheckInDate.Date <= DateTime.Today)
        {
            return (
                false,
                "This booking can no longer be cancelled."
            );
        }

        booking.Status =
            BookingStatus.Cancelled;

        booking.UpdatedAt =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (
            true,
            "Booking cancelled successfully."
        );
    }
}