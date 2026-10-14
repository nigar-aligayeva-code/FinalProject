using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class WishlistService : IWishlistService
{
    private readonly IAppDbContext _context;

    public WishlistService(IAppDbContext context)
    {
        _context = context;
    }


    // =========================
    // TOGGLE (SAVE / REMOVE)
    // =========================
    public async Task<(bool Success, bool Saved)> ToggleAsync(
        string appUserId,
        int roomId)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return (false, false);

        var roomExists = await _context.Rooms
            .AnyAsync(r =>
                r.Id == roomId &&
                !r.IsDeleted);

        if (!roomExists)
            return (false, false);

        var item = await _context.WishlistItems
            .FirstOrDefaultAsync(w =>
                w.AppUserId == appUserId &&
                w.RoomId == roomId);

        if (item != null)
        {
            // Artıq saxlanılıb -> sil
            _context.WishlistItems.Remove(item);

            await _context.SaveChangesAsync();

            return (true, false);
        }

        await _context.WishlistItems.AddAsync(new WishlistItem
        {
            AppUserId = appUserId,
            RoomId = roomId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // İki dəfə tez klik: unique index ikincisini dayandırır
            return (true, true);
        }

        return (true, true);
    }


    // =========================
    // SAVED ROOM IDS
    // =========================
    public async Task<HashSet<int>> GetSavedRoomIdsAsync(string? appUserId)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return new HashSet<int>();

        var ids = await _context.WishlistItems
            .Where(w => w.AppUserId == appUserId)
            .Select(w => w.RoomId)
            .ToListAsync();

        return ids.ToHashSet();
    }


    // =========================
    // SAVED ROOMS PAGE
    // =========================
    public async Task<List<FeaturedRoomDto>> GetSavedRoomsAsync(string appUserId)
    {
        return await _context.WishlistItems
            .Where(w =>
                w.AppUserId == appUserId &&
                !w.Room.IsDeleted &&
                !w.Room.Hotel.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new FeaturedRoomDto
            {
                Id = w.Room.Id,
                Name = w.Room.Name,
                RoomType = w.Room.RoomType,
                Capacity = w.Room.Capacity,
                PricePerNight = w.Room.PricePerNight,
                MainImage = w.Room.MainImage,

                HotelName = w.Room.Hotel.Name,
                City = w.Room.Hotel.City,
                Country = w.Room.Hotel.Country,

                ReviewCount = w.Room.Reviews
                    .Count(rv => !rv.IsDeleted),

                AverageRating = w.Room.Reviews
                    .Where(rv => !rv.IsDeleted)
                    .Select(rv => (double?)rv.Rating)
                    .Average() ?? 0
            })
            .ToListAsync();
    }
}
