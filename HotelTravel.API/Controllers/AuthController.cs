
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelTravel.API.DTOs.Auth;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HotelTravel.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
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

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(
                request.Email);

            if (user == null ||
                !await _userManager.CheckPasswordAsync(
                    user, request.Password))
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    user.Id),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    user.Email ?? string.Empty),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(
                    ClaimTypes.Role, role));
            }

            var jwtKey = _configuration["Jwt:Key"]!;
            var jwtIssuer = _configuration["Jwt:Issuer"]!;
            var jwtAudience = _configuration["Jwt:Audience"]!;

            var expirationMinutes = int.TryParse(
                _configuration["Jwt:ExpirationMinutes"],
                out var minutes) && minutes > 0
                    ? minutes
                    : 60;

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    expirationMinutes),
                signingCredentials: credentials);

            return Ok(new
            {
                accessToken =
                    new JwtSecurityTokenHandler()
                        .WriteToken(token),

                tokenType = "Bearer",

                expiresIn = expirationMinutes * 60,

                roles
            });
        }
    }
}
