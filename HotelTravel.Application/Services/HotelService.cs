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
                HotelChainId = h.HotelChainId
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
                HotelChainId = h.HotelChainId
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

        await _context.SaveChangesAsync();

        return true;
    }
}