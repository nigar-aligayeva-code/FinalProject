using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

/// <summary>
/// Otelin real boş yerləri (müəllimin qeydi: "otel təkcə bizimlə satmır").
/// Otel partnyoru başqa yerdə satdığı günləri bağlayır, qiymətləri yeniləyir və
/// "yoxladım" qeyd edir; sorğunu təsdiqləyən də odur. Admin partnyoru olmayan otellər üçün eyni işi görür.
/// </summary>
public class HotelAvailabilityService : IHotelAvailabilityService
{
    private readonly IAppDbContext _context;

    public HotelAvailabilityService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<HotelAvailabilityDto?> GetAsync(int hotelId)
    {
        var hotel = await _context.Hotels
            .FirstOrDefaultAsync(h => h.Id == hotelId && !h.IsDeleted);

        if (hotel is null)
            return null;

        var today = DateTime.Today;

        var rooms = await _context.Rooms
            .Where(r => r.HotelId == hotelId && !r.IsDeleted)
            .OrderBy(r => r.RoomNumber)
            .Select(r => new AvailabilityRoomDto
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Name = r.Name,
                RoomType = r.RoomType.ToString(),
                Capacity = r.Capacity,
                PricePerNight = r.PricePerNight,
                MainImage = r.MainImage,
                UpcomingBookings = r.Bookings.Count(b =>
                    !b.IsDeleted &&
                    b.Status != BookingStatus.Cancelled &&
                    b.Status != BookingStatus.CheckedOut &&
                    b.CheckOutDate >= today)
            })
            .ToListAsync();

        var roomIds = rooms.Select(r => r.Id).ToList();

        var blocks = await _context.RoomBlocks
            .Where(b => !b.IsDeleted && roomIds.Contains(b.RoomId) && b.EndDate > today)
            .OrderBy(b => b.StartDate)
            .ThenBy(b => b.Room.RoomNumber)
            .Select(b => new RoomBlockDto
            {
                Id = b.Id,
                RoomId = b.RoomId,
                RoomNumber = b.Room.RoomNumber,
                RoomName = b.Room.Name,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Reason = b.Reason,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        foreach (var room in rooms)
        {
            room.ClosedToday = blocks.Any(b =>
                b.RoomId == room.Id &&
                AvailabilityRules.Overlaps(b.StartDate, b.EndDate, today, today.AddDays(1)));
        }

        return new HotelAvailabilityDto
        {
            HotelId = hotel.Id,
            HotelName = hotel.Name,
            City = hotel.City,
            AvailabilityUpdatedAt = hotel.AvailabilityUpdatedAt,
            IsFresh = AvailabilityRules.IsFresh(hotel.AvailabilityUpdatedAt, DateTime.UtcNow),
            PendingRequests = await _context.Bookings.CountAsync(b =>
                !b.IsDeleted && b.Status == BookingStatus.Pending && b.Room.HotelId == hotelId),
            PartnerCount = await _context.Users.CountAsync(u => u.PartnerHotelId == hotelId),
            Rooms = rooms,
            Blocks = blocks
        };
    }

    public async Task<List<GetBookingDto>> GetBookingsAsync(int hotelId, BookingStatus? status = null)
    {
        var query = _context.Bookings
            .Where(b => !b.IsDeleted && b.Room.HotelId == hotelId);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        var today = DateTime.Today;

        return await query
            // Cavab gözləyənlər yuxarıda, sonra yaxın girişlər
            .OrderByDescending(b => b.Status == BookingStatus.Pending)
            .ThenBy(b => b.CheckInDate < today)
            .ThenBy(b => b.CheckInDate)
            .Take(200)
            .Select(b => new GetBookingDto
            {
                Id = b.Id,
                ConfirmationCode = b.ConfirmationCode,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                AdultCount = b.AdultCount,
                ChildrenCount = b.ChildrenCount,
                TotalPrice = b.TotalPrice,
                OriginalPrice = b.OriginalPrice,
                DiscountPercent = b.DiscountPercent,
                DiscountAmount = b.DiscountAmount,
                DiscountNote = b.DiscountNote,
                Status = b.Status,
                PaymentStatus = b.PaymentStatus,
                PaidAt = b.PaidAt,
                CreatedAt = b.CreatedAt,
                CancellationFee = b.CancellationFee,
                RefundAmount = b.RefundAmount,
                CancelledAt = b.CancelledAt,
                RoomId = b.RoomId,
                RoomName = b.Room.Name,
                RoomNumber = b.Room.RoomNumber,
                RoomImage = b.Room.MainImage,
                HotelId = b.Room.HotelId,
                HotelName = b.Room.Hotel.Name,
                HotelCity = b.Room.Hotel.City,
                GuestId = b.GuestId,
                GuestName = b.Guest.FirstName + " " + b.Guest.LastName,
                GuestEmail = b.Guest.Email
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> AddBlockAsync(int hotelId, CreateRoomBlockDto dto, string? userId = null)
    {
        var error = AvailabilityRules.ValidateBlock(dto.StartDate, dto.EndDate, DateTime.Today);

        if (error != null)
            return (false, error);

        var rooms = await _context.Rooms
            .Where(r => r.HotelId == hotelId && !r.IsDeleted &&
                        (dto.RoomId == null || r.Id == dto.RoomId))
            .Select(r => r.Id)
            .ToListAsync();

        if (rooms.Count == 0)
            return (false, "Room was not found.");

        var reason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim();

        foreach (var roomId in rooms)
        {
            _context.RoomBlocks.Add(new RoomBlock
            {
                RoomId = roomId,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                Reason = reason,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await TouchAsync(hotelId);
        await _context.SaveChangesAsync();

        return (true, rooms.Count == 1
            ? "The dates are closed. The room will not be shown for these days."
            : "The dates are closed for all rooms of the hotel.");
    }

    public async Task<(bool Success, string Message)> RemoveBlockAsync(int hotelId, int blockId)
    {
        var block = await _context.RoomBlocks
            .FirstOrDefaultAsync(b => b.Id == blockId && !b.IsDeleted && b.Room.HotelId == hotelId);

        if (block is null)
            return (false, "Closed dates were not found.");

        block.IsDeleted = true;
        block.UpdatedAt = DateTime.UtcNow;

        await TouchAsync(hotelId);
        await _context.SaveChangesAsync();

        return (true, "The dates are open again.");
    }

    public async Task<bool> MarkUpToDateAsync(int hotelId)
    {
        if (!await _context.Hotels.AnyAsync(h => h.Id == hotelId && !h.IsDeleted))
            return false;

        await TouchAsync(hotelId);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<(bool Success, string Message)> UpdateRoomPriceAsync(int hotelId, int roomId, decimal pricePerNight)
    {
        if (pricePerNight < 1 || pricePerNight > 100000)
            return (false, "Price must be between 1 and 100000.");

        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == roomId && r.HotelId == hotelId && !r.IsDeleted);

        if (room is null)
            return (false, "Room was not found.");

        room.PricePerNight = Math.Round(pricePerNight, 2);
        room.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "The price was updated. New bookings use the new price.");
    }

    public async Task<AvailabilityCheckDto?> CheckBookingAsync(int bookingId)
    {
        var booking = await _context.Bookings
            .Where(b => b.Id == bookingId && !b.IsDeleted)
            .Select(b => new
            {
                b.Id,
                b.RoomId,
                b.CheckInDate,
                b.CheckOutDate,
                b.Room.HotelId,
                HotelName = b.Room.Hotel.Name,
                b.Room.Hotel.AvailabilityUpdatedAt
            })
            .FirstOrDefaultAsync();

        if (booking is null)
            return null;

        var overlapping = await _context.Bookings.CountAsync(b =>
            b.Id != booking.Id &&
            b.RoomId == booking.RoomId &&
            !b.IsDeleted &&
            b.Status != BookingStatus.Cancelled &&
            b.CheckInDate < booking.CheckOutDate &&
            b.CheckOutDate > booking.CheckInDate);

        var reasons = await _context.RoomBlocks
            .Where(x =>
                !x.IsDeleted &&
                x.RoomId == booking.RoomId &&
                x.StartDate < booking.CheckOutDate &&
                x.EndDate > booking.CheckInDate)
            .Select(x => x.Reason ?? "")
            .ToListAsync();

        return new AvailabilityCheckDto
        {
            BookingId = booking.Id,
            HotelId = booking.HotelId,
            HotelName = booking.HotelName,
            OverlappingBookings = overlapping,
            IsClosedByHotel = reasons.Count > 0,
            ClosedReasons = reasons.Where(r => r.Length > 0).Distinct().ToList(),
            AvailabilityUpdatedAt = booking.AvailabilityUpdatedAt,
            IsFresh = AvailabilityRules.IsFresh(booking.AvailabilityUpdatedAt, DateTime.UtcNow),
            HasPartner = await _context.Users.AnyAsync(u => u.PartnerHotelId == booking.HotelId)
        };
    }

    // Otel boş yerlərlə işləyib -> "təzəlik" vaxtı yenilənir
    private async Task TouchAsync(int hotelId)
    {
        var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.Id == hotelId);

        if (hotel != null)
        {
            hotel.AvailabilityUpdatedAt = DateTime.UtcNow;
            hotel.UpdatedAt = DateTime.UtcNow;
        }
    }
}
