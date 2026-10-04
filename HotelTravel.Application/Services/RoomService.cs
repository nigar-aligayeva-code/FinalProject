using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class RoomService : IRoomService
{
    private readonly IAppDbContext _context;

    public RoomService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<GetRoomDto>> GetAllAsync()
    {
        return await _context.Rooms
            .Where(r => !r.IsDeleted)
            .Select(r => new GetRoomDto
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Name = r.Name,
                Description = r.Description,
                PricePerNight = r.PricePerNight,
                Capacity = r.Capacity,
                MainImage = r.MainImage,
                RoomType = r.RoomType,
                HotelId = r.HotelId,

                AmenityIds = r.Amenities
                    .Where(a => !a.IsDeleted)
                    .Select(a => a.Id)
                    .ToList(),

                AmenityNames = r.Amenities
                    .Where(a => !a.IsDeleted)
                    .Select(a => a.Name)
                    .ToList(),

                ImageUrls = r.Images
                    .Where(i => !i.IsDeleted)
                    .Select(i => i.ImageUrl)
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<GetRoomDto?> GetByIdAsync(int id)
    {
        return await _context.Rooms
            .Where(r =>
                r.Id == id &&
                !r.IsDeleted)
            .Select(r => new GetRoomDto
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Name = r.Name,
                Description = r.Description,
                PricePerNight = r.PricePerNight,
                Capacity = r.Capacity,
                MainImage = r.MainImage,
                RoomType = r.RoomType,
                HotelId = r.HotelId,

                AmenityIds = r.Amenities
                    .Where(a => !a.IsDeleted)
                    .Select(a => a.Id)
                    .ToList(),

                AmenityNames = r.Amenities
                    .Where(a => !a.IsDeleted)
                    .Select(a => a.Name)
                    .ToList(),

                ImageUrls = r.Images
                    .Where(i => !i.IsDeleted)
                    .Select(i => i.ImageUrl)
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(CreateRoomDto dto)
    {
        var amenities = await _context.Amenities
            .Where(a =>
                dto.AmenityIds.Contains(a.Id) &&
                !a.IsDeleted)
            .ToListAsync();

        var room = new Room
        {
            RoomNumber = dto.RoomNumber,
            Name = dto.Name,
            Description = dto.Description,
            PricePerNight = dto.PricePerNight,
            Capacity = dto.Capacity,
            MainImage = dto.MainImage,
            RoomType = dto.RoomType,
            HotelId = dto.HotelId,

            Amenities = amenities,

            Images = dto.ImageUrls
                .Select(imageUrl => new RoomImage
                {
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                })
                .ToList(),

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _context.Rooms.AddAsync(room);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateRoomDto dto)
    {
        var room = await _context.Rooms
            .Include(r => r.Amenities)
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                !r.IsDeleted);

        if (room is null)
            return false;

        var amenities = await _context.Amenities
            .Where(a =>
                dto.AmenityIds.Contains(a.Id) &&
                !a.IsDeleted)
            .ToListAsync();

        room.RoomNumber = dto.RoomNumber;
        room.Name = dto.Name;
        room.Description = dto.Description;
        room.PricePerNight = dto.PricePerNight;
        room.Capacity = dto.Capacity;
        room.MainImage = dto.MainImage;
        room.RoomType = dto.RoomType;
        room.HotelId = dto.HotelId;

        // AMENITIES
        room.Amenities.Clear();

        foreach (var amenity in amenities)
        {
            room.Amenities.Add(amenity);
        }

        // NEW GALLERY IMAGES
        foreach (var imageUrl in dto.ImageUrls)
        {
            var imageExists = room.Images.Any(i =>
                !i.IsDeleted &&
                i.ImageUrl == imageUrl);

            if (!imageExists)
            {
                room.Images.Add(new RoomImage
                {
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }
        }

        room.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                !r.IsDeleted);

        if (room is null)
            return false;

        room.IsDeleted = true;
        room.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<GetRoomDto>> SearchAvailableRoomsAsync(
        RoomSearchDto search)
    {
        if (search.CheckInDate.Date >= search.CheckOutDate.Date)
            return new List<GetRoomDto>();

        var totalGuests =
            search.AdultCount + search.ChildrenCount;

        if (search.AdultCount <= 0 ||
            search.ChildrenCount < 0)
            return new List<GetRoomDto>();

        return await _context.Rooms
            .Where(r =>
                !r.IsDeleted &&

                r.Capacity >= totalGuests &&

                !r.Bookings.Any(b =>
                    !b.IsDeleted &&
                    b.Status != HotelTravel.Domain.Enums.BookingStatus.Cancelled &&
                    search.CheckInDate.Date < b.CheckOutDate.Date &&
                    search.CheckOutDate.Date > b.CheckInDate.Date
                )
            )
            .Select(r => new GetRoomDto
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Name = r.Name,
                Description = r.Description,
                PricePerNight = r.PricePerNight,
                Capacity = r.Capacity,
                MainImage = r.MainImage,
                RoomType = r.RoomType,
                HotelId = r.HotelId,

                AmenityIds = r.Amenities
                    .Where(a => !a.IsDeleted)
                    .Select(a => a.Id)
                    .ToList(),

                AmenityNames = r.Amenities
                    .Where(a => !a.IsDeleted)
                    .Select(a => a.Name)
                    .ToList(),

                ImageUrls = r.Images
                    .Where(i => !i.IsDeleted)
                    .Select(i => i.ImageUrl)
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<List<DateTime>> GetFullyBookedDatesAsync(
        DateTime startDate,
        DateTime endDate)
    {
        var fullyBookedDates = new List<DateTime>();

        var rooms = await _context.Rooms
            .Where(r => !r.IsDeleted)
            .ToListAsync();

        if (rooms.Count == 0)
            return fullyBookedDates;

        var bookings = await _context.Bookings
            .Where(b =>
                !b.IsDeleted &&
                b.Status != HotelTravel.Domain.Enums.BookingStatus.Cancelled &&
                b.CheckInDate < endDate &&
                b.CheckOutDate > startDate)
            .ToListAsync();

        for (var date = startDate.Date;
             date <= endDate.Date;
             date = date.AddDays(1))
        {
            var bookedRoomCount = bookings
                .Where(b =>
                    date >= b.CheckInDate.Date &&
                    date < b.CheckOutDate.Date)
                .Select(b => b.RoomId)
                .Distinct()
                .Count();

            if (bookedRoomCount >= rooms.Count)
            {
                fullyBookedDates.Add(date);
            }
        }

        return fullyBookedDates;
    }
}