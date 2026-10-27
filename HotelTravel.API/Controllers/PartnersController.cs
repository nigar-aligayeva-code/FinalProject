using HotelTravel.API.Services;
using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.API.Controllers;

/// <summary>Admin: hotel partner accounts (hotel staff who confirm requests and manage availability of one hotel).</summary>
[Route("api/partners")]
[Authorize(Roles = "Admin")]
public class PartnersController : ApiControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly IAppDbContext _context;

    public PartnersController(UserManager<AppUser> users, RoleManager<IdentityRole> roles, IAppDbContext context)
    {
        _users = users;
        _roles = roles;
        _context = context;
    }

    // GET: api/partners
    /// <summary>All partner accounts with their hotel.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PartnerAccountDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var partners = await _users.GetUsersInRoleAsync(PartnerScope.PartnerRole);

        var hotelIds = partners.Where(p => p.PartnerHotelId.HasValue).Select(p => p.PartnerHotelId!.Value).Distinct().ToList();

        var hotels = await _context.Hotels
            .Where(h => hotelIds.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id, h => h.Name);

        return Ok(partners
            .OrderBy(p => p.PartnerHotelId)
            .ThenBy(p => p.Email)
            .Select(p => new PartnerAccountDto
            {
                UserId = p.Id,
                Email = p.Email ?? string.Empty,
                FullName = $"{p.FirstName} {p.LastName}".Trim(),
                HotelId = p.PartnerHotelId ?? 0,
                HotelName = p.PartnerHotelId.HasValue && hotels.TryGetValue(p.PartnerHotelId.Value, out var name) ? name : "—"
            })
            .ToList());
    }

    // POST: api/partners
    /// <summary>Create a partner account for a hotel (email is confirmed, the hotel gets the password from the admin).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> Create([FromBody] CreatePartnerDto dto)
    {
        if (!await _context.Hotels.AnyAsync(h => h.Id == dto.HotelId && !h.IsDeleted))
            return BadRequest(ApiResultDto.Fail("Hotel not found."));

        if (await _users.FindByEmailAsync(dto.Email.Trim()) != null)
            return BadRequest(ApiResultDto.Fail("An account with this email already exists."));

        if (!await _roles.RoleExistsAsync(PartnerScope.PartnerRole))
            await _roles.CreateAsync(new IdentityRole(PartnerScope.PartnerRole));

        var user = new AppUser
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            UserName = dto.Email.Trim(),
            EmailConfirmed = true,
            PartnerHotelId = dto.HotelId
        };

        var created = await _users.CreateAsync(user, dto.Password);

        if (!created.Succeeded)
            return BadRequest(ApiResultDto.Fail(string.Join(" ", created.Errors.Select(e => e.Description))));

        await _users.AddToRoleAsync(user, PartnerScope.PartnerRole);

        return Ok(ApiResultDto.Ok("Partner account created."));
    }

    // DELETE: api/partners/{userId}
    /// <summary>Remove partner access (the account stays as a normal user).</summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> Remove(string userId)
    {
        var user = await _users.FindByIdAsync(userId);

        if (user is null || !await _users.IsInRoleAsync(user, PartnerScope.PartnerRole))
            return NotFound(ApiResultDto.Fail("Partner not found."));

        await _users.RemoveFromRoleAsync(user, PartnerScope.PartnerRole);

        user.PartnerHotelId = null;
        await _users.UpdateAsync(user);

        if (!await _users.IsInRoleAsync(user, "User"))
            await _users.AddToRoleAsync(user, "User");

        return Ok(ApiResultDto.Ok("Partner access removed."));
    }
}
