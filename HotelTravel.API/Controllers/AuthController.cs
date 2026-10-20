using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelTravel.API.DTOs.Auth;
using HotelTravel.Application.DTOs.Account;
using HotelTravel.Application.DTOs.Auth;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HotelTravel.API.Controllers;

// Giriş və qeydiyyat: JWT token qaytarır
/// <summary>Login and registration. Returns a JWT access token used by the website and Swagger.</summary>
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ApiControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<AppUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    // POST: api/auth/login
    /// <summary>Sign in with email and password. Copy accessToken from the response and click Authorize.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null ||
            !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(ApiResultDto.Fail("Email or password is incorrect."));
        }

        return Ok(await CreateTokenAsync(user));
    }

    // POST: api/auth/register
    /// <summary>Create a new user account and sign in.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);

        if (existing != null)
            return BadRequest(ApiResultDto.Fail("An account with this email already exists."));

        var user = new AppUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            UserName = request.Email.Trim()
        };

        var created = await _userManager.CreateAsync(user, request.Password);

        if (!created.Succeeded)
        {
            return BadRequest(ApiResultDto.Fail(
                string.Join(" ", created.Errors.Select(e => e.Description))));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "User");

        if (!roleResult.Succeeded)
        {
            return BadRequest(ApiResultDto.Fail(
                string.Join(" ", roleResult.Errors.Select(e => e.Description))));
        }

        return Ok(await CreateTokenAsync(user));
    }

    // GET: api/auth/me  (token yoxlamaq üçün)
    /// <summary>Current user's profile from the token (useful to test the token).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId ?? string.Empty);

        if (user == null)
            return Unauthorized(ApiResultDto.Fail("User not found."));

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            AvatarKey = user.AvatarKey,
            Roles = roles.ToList()
        });
    }


    // =========================
    // TOKEN
    // =========================
    private async Task<AuthResponseDto> CreateTokenAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.Email ?? string.Empty),
            new(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
            new(ClaimTypes.Surname, user.LastName ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expirationMinutes =
            int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var minutes) && minutes > 0
                ? minutes
                : 60;

        var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new AuthResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            TokenType = "Bearer",
            ExpiresAtUtc = expires,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            AvatarKey = user.AvatarKey,
            Roles = roles.ToList()
        };
    }
}
