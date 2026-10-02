using HotelTravel.Application.DTOs.Amenities;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class AmenityService : IAmenityService
{
    private readonly IAppDbContext _context;

    public AmenityService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<GetAmenityDto>> GetAllAsync()
    {
        return await _context.Amenities
            .Where(a => !a.IsDeleted)
            .Select(a => new GetAmenityDto
            {
                Id = a.Id,
                Name = a.Name,
                Icon = a.Icon
            })
            .ToListAsync();
    }

    public async Task<GetAmenityDto?> GetByIdAsync(int id)
    {
        return await _context.Amenities
            .Where(a =>
                a.Id == id &&
                !a.IsDeleted)
            .Select(a => new GetAmenityDto
            {
                Id = a.Id,
                Name = a.Name,
                Icon = a.Icon
            })
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(CreateAmenityDto dto)
    {
        var amenity = new Amenity
        {
            Name = dto.Name,
            Icon = dto.Icon,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _context.Amenities.AddAsync(amenity);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateAmenityDto dto)
    {
        var amenity = await _context.Amenities
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                !a.IsDeleted);

        if (amenity is null)
            return false;

        amenity.Name = dto.Name;
        amenity.Icon = dto.Icon;
        amenity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var amenity = await _context.Amenities
            .Include(a => a.Rooms)
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                !a.IsDeleted);

        if (amenity is null)
        {
            return (
                false,
                "Amenity not found."
            );
        }

        var isUsedByRoom = amenity.Rooms
            .Any(r => !r.IsDeleted);

        if (isUsedByRoom)
        {
            return (
                false,
                "This amenity cannot be deleted because it is used by an active room."
            );
        }

        amenity.IsDeleted = true;
        amenity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (
            true,
            "Amenity deleted successfully."
        );
    }
}