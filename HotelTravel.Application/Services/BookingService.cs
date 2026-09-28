using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class BookingService : IBookingService
{
    private readonly IAppDbContext _context;

    public BookingService(IAppDbContext context)
    {
        _context = context;
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
        // 1. Tarix yoxlaması
        if (dto.CheckInDate.Date >= dto.CheckOutDate.Date)
        {
            return (
                false,
                "Check-out date must be after check-in date.",
                null
            );
        }

        // 2. Room mövcuddur?
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r =>
                r.Id == dto.RoomId &&
                !r.IsDeleted);

        if (room is null)
        {
            return (
                false,
                "Room not found.",
                null
            );
        }

        // 3. Qonaq sayı otağın capacity-sini keçirmi?
        var totalGuests = dto.AdultCount + dto.ChildrenCount;

        if (dto.AdultCount <= 0 ||
            dto.ChildrenCount < 0 ||
            totalGuests > room.Capacity)
        {
            return (
                false,
                "Guest count is not valid for this room.",
                null
            );
        }

        // 4. Eyni tarixlərdə aktiv booking varmı?
        var roomIsBooked = await _context.Bookings
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
                null
            );
        }

        // 5. Gecə sayını hesabla
        var numberOfNights =
            (dto.CheckOutDate.Date - dto.CheckInDate.Date).Days;

        // 6. Total price hesabla
        var totalPrice =
            numberOfNights * room.PricePerNight;

        // 7. Guest yarat
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

        await _context.Guests.AddAsync(guest);

        // 8. Confirmation code yarat
        var confirmationCode =
            $"BK-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // 9. Booking yarat
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

        await _context.Bookings.AddAsync(booking);

        // 10. Database-ə yaz
        await _context.SaveChangesAsync();

        return (
            true,
            "Booking created successfully.",
            confirmationCode
        );
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
}