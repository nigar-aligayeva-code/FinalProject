using HotelTravel.Application.DTOs.PromoCodes;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class PromoCodeService : IPromoCodeService
{
    private readonly IAppDbContext _context;

    public PromoCodeService(IAppDbContext context)
    {
        _context = context;
    }


    // =========================
    // LIST
    // =========================
    public async Task<List<GetPromoCodeDto>> GetAllAsync()
    {
        var today = DateTime.Today;

        var promos = await _context.PromoCodes
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return promos
            .Select(p =>
            {
                var error = BookingRules.ValidatePromo(
                    p.IsActive,
                    p.IsDeleted,
                    p.ValidFrom,
                    p.ValidUntil,
                    p.MaxUses,
                    p.UsedCount,
                    today);

                return new GetPromoCodeDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Description = p.Description,
                    DiscountPercent = p.DiscountPercent,
                    ValidFrom = p.ValidFrom,
                    ValidUntil = p.ValidUntil,
                    MaxUses = p.MaxUses,
                    UsedCount = p.UsedCount,
                    IsActive = p.IsActive,
                    IsUsableToday = error == null,
                    StatusText = error == null ? "Active" : error
                };
            })
            .ToList();
    }


    // =========================
    // CREATE
    // =========================
    public async Task<(bool Success, string Message)> CreateAsync(
        CreatePromoCodeDto dto)
    {
        var code = dto.Code.Trim().ToUpper();

        if (dto.ValidFrom.HasValue &&
            dto.ValidUntil.HasValue &&
            dto.ValidUntil.Value.Date < dto.ValidFrom.Value.Date)
        {
            return (false, "'Valid until' must be after 'Valid from'.");
        }

        // Silinmiş kod da daxil - unique index var
        var exists = await _context.PromoCodes
            .AnyAsync(p => p.Code == code);

        if (exists)
            return (false, $"Promo code {code} already exists.");

        await _context.PromoCodes.AddAsync(new PromoCode
        {
            Code = code,
            Description = dto.Description?.Trim(),
            DiscountPercent = dto.DiscountPercent,
            ValidFrom = dto.ValidFrom?.Date,
            ValidUntil = dto.ValidUntil?.Date,
            MaxUses = dto.MaxUses,
            UsedCount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        await _context.SaveChangesAsync();

        return (true, $"Promo code {code} created.");
    }


    // =========================
    // ACTIVATE / DEACTIVATE
    // =========================
    public async Task<bool> ToggleActiveAsync(int id)
    {
        var promo = await _context.PromoCodes
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (promo is null)
            return false;

        promo.IsActive = !promo.IsActive;
        promo.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }


    // =========================
    // DELETE (SOFT)
    // =========================
    public async Task<bool> DeleteAsync(int id)
    {
        var promo = await _context.PromoCodes
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (promo is null)
            return false;

        promo.IsDeleted = true;
        promo.IsActive = false;
        promo.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}
