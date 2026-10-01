using HotelTravel.Application.DTOs.HotelChains;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class HotelChainService : IHotelChainService
{
    private readonly IAppDbContext _context;

    public HotelChainService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<GetHotelChainDto>> GetAllAsync()
    {
        return await _context.HotelChains
            .Where(hc => !hc.IsDeleted)
            .Select(hc => new GetHotelChainDto
            {
                Id = hc.Id,
                Name = hc.Name,
                Description = hc.Description,
                Logo = hc.Logo
            })
            .ToListAsync();
    }

    public async Task<GetHotelChainDto?> GetByIdAsync(int id)
    {
        return await _context.HotelChains
            .Where(hc =>
                hc.Id == id &&
                !hc.IsDeleted)
            .Select(hc => new GetHotelChainDto
            {
                Id = hc.Id,
                Name = hc.Name,
                Description = hc.Description,
                Logo = hc.Logo
            })
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(CreateHotelChainDto dto)
    {
        var hotelChain = new HotelChain
        {
            Name = dto.Name,
            Description = dto.Description,
            Logo = dto.Logo,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _context.HotelChains.AddAsync(hotelChain);

        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateHotelChainDto dto)
    {
        var hotelChain =
            await _context.HotelChains
                .FirstOrDefaultAsync(hc =>
                    hc.Id == id &&
                    !hc.IsDeleted);

        if (hotelChain is null)
            return false;

        hotelChain.Name = dto.Name;
        hotelChain.Description = dto.Description;
        hotelChain.Logo = dto.Logo;
        hotelChain.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var hotelChain =
            await _context.HotelChains
                .FirstOrDefaultAsync(hc =>
                    hc.Id == id &&
                    !hc.IsDeleted);

        if (hotelChain is null)
            return false;

        hotelChain.IsDeleted = true;
        hotelChain.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}