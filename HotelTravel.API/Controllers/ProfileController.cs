using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Profile;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

// Daxil olmuş istifadəçinin profili (məlumat: GET api/auth/me)
[Route("api/profile")]
[Authorize]
public class ProfileController : ApiControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IProfileService _profileService;

    public ProfileController(
        UserManager<AppUser> userManager,
        IProfileService profileService)
    {
        _userManager = userManager;
        _profileService = profileService;
    }

    // GET: api/profile/summary
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ProfileSummaryDto), 200)]
    public async Task<IActionResult> Summary()
        => Ok(await _profileService.GetSummaryAsync(CurrentUserId!));

    // PUT: api/profile
    [HttpPut]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> Update([FromBody] UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId ?? string.Empty);

        if (user == null)
            return Unauthorized(ApiResultDto.Fail("User not found."));

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber)
            ? null
            : dto.PhoneNumber.Trim();

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded
            ? Ok(ApiResultDto.Ok("Profile updated."))
            : BadRequest(ApiResultDto.Fail(string.Join(" ", result.Errors.Select(e => e.Description))));
    }

    // PUT: api/profile/avatar
    [HttpPut("avatar")]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarDto dto)
    {
        if (!AvatarCatalog.IsValid(dto.AvatarKey))
            return BadRequest(ApiResultDto.Fail("Unknown avatar."));

        var user = await _userManager.FindByIdAsync(CurrentUserId ?? string.Empty);

        if (user == null)
            return Unauthorized(ApiResultDto.Fail("User not found."));

        user.AvatarKey = AvatarCatalog.Normalize(dto.AvatarKey);

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded
            ? Ok(ApiResultDto.Ok(user.AvatarKey == null ? "Avatar removed." : "Avatar updated."))
            : BadRequest(ApiResultDto.Fail(string.Join(" ", result.Errors.Select(e => e.Description))));
    }

    // POST: api/profile/change-password
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId ?? string.Empty);

        if (user == null)
            return Unauthorized(ApiResultDto.Fail("User not found."));

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

        if (result.Succeeded)
            return Ok(ApiResultDto.Ok("Password changed."));

        // Identity "PasswordMismatch" kodunu daha aydın mesajla qaytarırıq
        var message = result.Errors.Any(e => e.Code == "PasswordMismatch")
            ? "Current password is incorrect."
            : string.Join(" ", result.Errors.Select(e => e.Description));

        return BadRequest(ApiResultDto.Fail(message));
    }
}
