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


    // =========================
    // GET ALL ROOMS
    // =========================
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
                PanoramaImage = r.PanoramaImage,
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
                    .ToList(),

                Images = r.Images
                    .Where(i => !i.IsDeleted)
                    .Select(i => new GetRoomImageDto
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl
                    })
                    .ToList()
            })
            .ToListAsync();
    }


    // =========================
    // GET ROOM BY ID
    // =========================
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
                PanoramaImage = r.PanoramaImage,
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
                    .ToList(),

                Images = r.Images
                    .Where(i => !i.IsDeleted)
                    .Select(i => new GetRoomImageDto
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }


    // =========================
    // CREATE ROOM
    // =========================
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
            PanoramaImage = dto.PanoramaImage,

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


    // =========================
    // UPDATE ROOM
    // =========================
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


        // =========================
        // AMENITIES
        // =========================
        var amenities = await _context.Amenities
            .Where(a =>
                dto.AmenityIds.Contains(a.Id) &&
                !a.IsDeleted)
            .ToListAsync();


        // =========================
        // ROOM INFORMATION
        // =========================
        room.RoomNumber = dto.RoomNumber;
        room.Name = dto.Name;
        room.Description = dto.Description;
        room.PricePerNight = dto.PricePerNight;
        room.Capacity = dto.Capacity;

        room.MainImage = dto.MainImage;
        room.PanoramaImage = dto.PanoramaImage;

        room.RoomType = dto.RoomType;
        room.HotelId = dto.HotelId;


        // =========================
        // UPDATE AMENITIES
        // =========================
        room.Amenities.Clear();

        foreach (var amenity in amenities)
        {
            room.Amenities.Add(amenity);
        }


        // =========================
        // ADD NEW GALLERY IMAGES
        // =========================
        foreach (var imageUrl in dto.ImageUrls)
        {
            var imageExists =
                room.Images.Any(i =>
                    !i.IsDeleted &&
                    i.ImageUrl == imageUrl);

            if (!imageExists)
            {
                room.Images.Add(
                    new RoomImage
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


    // =========================
    // DELETE ROOM
    // =========================
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


    // =========================
    // GET GALLERY IMAGE BY ID
    // =========================
    public async Task<GetRoomImageDto?> GetImageByIdAsync(int imageId)
    {
        return await _context.RoomImages
            .Where(i =>
                i.Id == imageId &&
                !i.IsDeleted)
            .Select(i => new GetRoomImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl
            })
            .FirstOrDefaultAsync();
    }


    // =========================
    // DELETE GALLERY IMAGE
    // =========================
    public async Task<bool> DeleteImageAsync(int imageId)
    {
        var image = await _context.RoomImages
            .FirstOrDefaultAsync(i =>
                i.Id == imageId &&
                !i.IsDeleted);

        if (image is null)
            return false;

        image.IsDeleted = true;
        image.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }


    // =========================
    // DELETE MAIN IMAGE
    // =========================
    public async Task<bool> DeleteMainImageAsync(int roomId)
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r =>
                r.Id == roomId &&
                !r.IsDeleted);

        if (room is null)
            return false;

        room.MainImage = string.Empty;
        room.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }


    // =========================
    // SEARCH AVAILABLE ROOMS
    // =========================
    public async Task<List<GetRoomDto>>
        SearchAvailableRoomsAsync(RoomSearchDto search)
    {
        // =========================
        // CHECK DATES
        // =========================
        if (!search.CheckInDate.HasValue ||
            !search.CheckOutDate.HasValue)
        {
            return new List<GetRoomDto>();
        }

        var checkInDate =
            search.CheckInDate.Value.Date;

        var checkOutDate =
            search.CheckOutDate.Value.Date;


        // =========================
        // CHECK DATE RANGE
        // =========================
        if (checkInDate >= checkOutDate)
        {
            return new List<GetRoomDto>();
        }


        // =========================
        // CHECK GUEST COUNT
        // =========================
        var totalGuests =
            search.AdultCount +
            search.ChildrenCount;

        if (search.AdultCount <= 0 ||
            search.ChildrenCount < 0)
        {
            return new List<GetRoomDto>();
        }


        // =========================
        // BASE QUERY
        // =========================
        var query = _context.Rooms
            .Where(r =>
                !r.IsDeleted &&

                r.Capacity >= totalGuests &&

                !r.Bookings.Any(b =>
                    !b.IsDeleted &&

                    b.Status !=
                        HotelTravel.Domain.Enums
                            .BookingStatus.Cancelled &&

                    checkInDate <
                        b.CheckOutDate.Date &&

                    checkOutDate >
                        b.CheckInDate.Date
                )
            );


        // =========================
        // DESTINATION FILTER
        // =========================
        if (!string.IsNullOrWhiteSpace(
            search.Destination))
        {
            var destination =
                search.Destination.Trim();

            query = query.Where(r =>
                r.Hotel.City.Contains(destination) ||
                r.Hotel.Country.Contains(destination) ||
                r.Hotel.Name.Contains(destination));
        }


        // =========================
        // RESULT
        // =========================
        return await query
            .Select(r => new GetRoomDto
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Name = r.Name,
                Description = r.Description,
                PricePerNight = r.PricePerNight,
                Capacity = r.Capacity,

                MainImage = r.MainImage,
                PanoramaImage = r.PanoramaImage,

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
                    .ToList(),

                Images = r.Images
                    .Where(i => !i.IsDeleted)
                    .Select(i => new GetRoomImageDto
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl
                    })
                    .ToList()
            })
            .ToListAsync();
    }


    // =========================
    // FULLY BOOKED DATES
    // =========================
    public async Task<List<DateTime>>
        GetFullyBookedDatesAsync(
            DateTime startDate,
            DateTime endDate)
    {
        var fullyBookedDates =
            new List<DateTime>();

        var rooms = await _context.Rooms
            .Where(r => !r.IsDeleted)
            .ToListAsync();

        if (rooms.Count == 0)
            return fullyBookedDates;


        var bookings = await _context.Bookings
            .Where(b =>
                !b.IsDeleted &&

                b.Status !=
                    HotelTravel.Domain.Enums
                        .BookingStatus.Cancelled &&

                b.CheckInDate < endDate &&
                b.CheckOutDate > startDate)
            .ToListAsync();


        for (var date = startDate.Date;
             date <= endDate.Date;
             date = date.AddDays(1))
        {
            var bookedRoomCount =
                bookings
                    .Where(b =>
                        date >=
                        b.CheckInDate.Date &&

                        date <
                        b.CheckOutDate.Date)
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