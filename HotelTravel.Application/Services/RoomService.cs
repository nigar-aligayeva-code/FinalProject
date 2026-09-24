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
                HotelId = r.HotelId
            })
            .ToListAsync();
    }

    public async Task<GetRoomDto?> GetByIdAsync(int id)
    {
        return await _context.Rooms
            .Where(r => r.Id == id && !r.IsDeleted)
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
                HotelId = r.HotelId
            })
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(CreateRoomDto dto)
    {
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

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _context.Rooms.AddAsync(room);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(int id, UpdateRoomDto dto)
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (room is null)
            return false;

        room.RoomNumber = dto.RoomNumber;
        room.Name = dto.Name;
        room.Description = dto.Description;
        room.PricePerNight = dto.PricePerNight;
        room.Capacity = dto.Capacity;
        room.MainImage = dto.MainImage;
        room.RoomType = dto.RoomType;
        room.HotelId = dto.HotelId;
        room.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (room is null)
            return false;

        room.IsDeleted = true;
        room.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}