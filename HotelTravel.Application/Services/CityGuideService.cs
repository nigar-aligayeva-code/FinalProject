using HotelTravel.Application.DTOs.CityGuide;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class CityGuideService : ICityGuideService
{
    private readonly IAppDbContext _context;

    public CityGuideService(IAppDbContext context)
    {
        _context = context;
    }


    // =========================
    // PUBLIC
    // =========================
    public async Task<List<CityPlaceDto>> GetByCityAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return new List<CityPlaceDto>();

        var name = city.Trim().ToLower();

        return await Project(
                _context.CityPlaces.Where(p =>
                    !p.IsDeleted &&
                    p.City.ToLower() == name))
            .ToListAsync();
    }

    public async Task<List<string>> GetCitiesAsync()
    {
        return await _context.CityPlaces
            .Where(p => !p.IsDeleted)
            .Select(p => p.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }


    // =========================
    // ADMIN
    // =========================
    public async Task<List<CityPlaceDto>> GetAllAsync(string? city)
    {
        var query = _context.CityPlaces.Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var name = city.Trim().ToLower();
            query = query.Where(p => p.City.ToLower() == name);
        }

        return await Project(query.OrderBy(p => p.City))
            .ToListAsync();
    }

    public async Task<SaveCityPlaceDto?> GetForEditAsync(int id)
    {
        return await _context.CityPlaces
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => new SaveCityPlaceDto
            {
                Id = p.Id,
                City = p.City,
                Name = p.Name,
                Category = p.Category,
                Description = p.Description,
                Address = p.Address,
                ImageUrl = p.ImageUrl,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                PriceFrom = p.PriceFrom,
                Duration = p.Duration,
                SortOrder = p.SortOrder
            })
            .FirstOrDefaultAsync();
    }

    public async Task SaveAsync(SaveCityPlaceDto dto)
    {
        CityPlace? place = null;

        if (dto.Id.HasValue)
        {
            place = await _context.CityPlaces
                .FirstOrDefaultAsync(p => p.Id == dto.Id.Value && !p.IsDeleted);
        }

        if (place is null)
        {
            place = new CityPlace
            {
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _context.CityPlaces.AddAsync(place);
        }

        place.City = dto.City.Trim();
        place.Name = dto.Name.Trim();
        place.Category = dto.Category;
        place.Description = dto.Description.Trim();
        place.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
        place.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        place.Latitude = dto.Latitude;
        place.Longitude = dto.Longitude;
        place.PriceFrom = dto.PriceFrom;
        place.Duration = string.IsNullOrWhiteSpace(dto.Duration) ? null : dto.Duration.Trim();
        place.SortOrder = dto.SortOrder;
        place.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var place = await _context.CityPlaces
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (place is null)
            return false;

        place.IsDeleted = true;
        place.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }


    // =========================
    // HELPER
    // =========================
    private static IQueryable<CityPlaceDto> Project(IQueryable<CityPlace> query)
    {
        return query
            .OrderBy(p => p.Category)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new CityPlaceDto
            {
                Id = p.Id,
                City = p.City,
                Name = p.Name,
                Category = p.Category,
                Description = p.Description,
                Address = p.Address,
                ImageUrl = p.ImageUrl,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                PriceFrom = p.PriceFrom,
                Duration = p.Duration,
                SortOrder = p.SortOrder
            });
    }
}
