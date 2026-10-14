using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.DTOs.Hotels;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class HotelService : IHotelService
{
    private readonly IAppDbContext _context;

    public HotelService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<GetHotelDto>> GetAllAsync()
    {
        return await _context.Hotels
            .Where(h => !h.IsDeleted)
            .Select(h => new GetHotelDto
            {
                Id = h.Id,
                Name = h.Name,
                Description = h.Description,
                Address = h.Address,
                City = h.City,
                Country = h.Country,
                Phone = h.Phone,
                Email = h.Email,
                MainImage = h.MainImage,
                Latitude = h.Latitude,
                Longitude = h.Longitude,
                StarRating = h.StarRating,
                HotelChainId = h.HotelChainId,

                HotelChainName = h.HotelChain != null
                    ? h.HotelChain.Name
                    : null
            })
            .ToListAsync();
    }

    public async Task<GetHotelDto?> GetByIdAsync(int id)
    {
        return await _context.Hotels
            .Where(h => h.Id == id && !h.IsDeleted)
            .Select(h => new GetHotelDto
            {
                Id = h.Id,
                Name = h.Name,
                Description = h.Description,
                Address = h.Address,
                City = h.City,
                Country = h.Country,
                Phone = h.Phone,
                Email = h.Email,
                MainImage = h.MainImage,
                Latitude = h.Latitude,
                Longitude = h.Longitude,
                StarRating = h.StarRating,
                HotelChainId = h.HotelChainId,

                HotelChainName = h.HotelChain != null
                    ? h.HotelChain.Name
                    : null,

                Images = h.Images
                    .Where(i => !i.IsDeleted)
                    .Select(i => new GetHotelImageDto
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        Category = i.Category
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(CreateHotelDto dto)
    {
        var hotel = new Hotel
        {
            Name = dto.Name,
            Description = dto.Description,
            Address = dto.Address,
            City = dto.City,
            Country = dto.Country,
            Phone = dto.Phone,
            Email = dto.Email,
            MainImage = dto.MainImage,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            StarRating = dto.StarRating,
            HotelChainId = dto.HotelChainId,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _context.Hotels.AddAsync(hotel);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(int id, UpdateHotelDto dto)
    {
        var hotel = await _context.Hotels
            .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);

        if (hotel is null)
            return false;

        hotel.Name = dto.Name;
        hotel.Description = dto.Description;
        hotel.Address = dto.Address;
        hotel.City = dto.City;
        hotel.Country = dto.Country;
        hotel.Phone = dto.Phone;
        hotel.Email = dto.Email;
        hotel.MainImage = dto.MainImage;
        hotel.Latitude = dto.Latitude;
        hotel.Longitude = dto.Longitude;
        hotel.StarRating = dto.StarRating;
        hotel.HotelChainId = dto.HotelChainId;
        hotel.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var hotel = await _context.Hotels
            .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);

        if (hotel is null)
            return false;

        hotel.IsDeleted = true;
        hotel.UpdatedAt = DateTime.UtcNow;

        // Otel silinəndə onun otaqları da gizlənsin
        var rooms = await _context.Rooms
            .Where(r =>
                r.HotelId == id &&
                !r.IsDeleted)
            .ToListAsync();

        foreach (var room in rooms)
        {
            room.IsDeleted = true;
            room.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return true;
    }


    // =========================
    // UPCOMING BOOKINGS (silməzdən əvvəl yoxlama)
    // =========================
    public async Task<int> CountUpcomingBookingsAsync(int hotelId)
    {
        var today = DateTime.Today;

        return await _context.Bookings
            .CountAsync(b =>
                b.Room.HotelId == hotelId &&
                !b.IsDeleted &&
                (b.Status == HotelTravel.Domain.Enums.BookingStatus.Confirmed ||
                 b.Status == HotelTravel.Domain.Enums.BookingStatus.CheckedIn) &&
                b.CheckOutDate >= today);
    }


    // =========================
    // HOME - POPULAR DESTINATIONS
    // =========================
    public async Task<List<DestinationDto>> GetPopularDestinationsAsync(int count)
    {
        var hotels = await _context.Hotels
            .Where(h => !h.IsDeleted)
            .Select(h => new
            {
                h.City,
                h.Country,

                // Əvvəlcə "Overview" (bina) şəkli, yoxdursa MainImage
                ImageUrl = h.Images
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.Category == "Overview" ? 0 : 1)
                    .ThenBy(i => i.Id)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault() ?? h.MainImage,

                RoomCount = h.Rooms.Count(r => !r.IsDeleted),

                MinPrice = h.Rooms
                    .Where(r => !r.IsDeleted)
                    .Select(r => (decimal?)r.PricePerNight)
                    .Min()
            })
            .ToListAsync();

        return hotels
            .GroupBy(h => new { h.City, h.Country })
            .Select(g => new DestinationDto
            {
                City = g.Key.City,
                Country = g.Key.Country,
                ImageUrl = g.Select(h => h.ImageUrl)
                    .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url)),
                HotelCount = g.Count(),
                RoomCount = g.Sum(h => h.RoomCount),
                MinPrice = g.Min(h => h.MinPrice)
            })
            .Where(d => d.RoomCount > 0)
            .OrderByDescending(d => d.RoomCount)
            .ThenBy(d => d.City)
            .Take(count)
            .ToList();
    }
}
