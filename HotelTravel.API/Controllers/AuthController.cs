using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelTravel.API.DTOs.Auth;
using HotelTravel.Application.DTOs.Account;
using HotelTravel.Application.DTOs.Auth;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Options;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
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
    private readonly IEmailOutbox _outbox;
    private readonly EmailOptions _emailOptions;
    private readonly IEmailAddressChecker _emailChecker;

    public AuthController(
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        IEmailOutbox outbox,
        EmailOptions emailOptions,
        IEmailAddressChecker emailChecker)
    {
        _userManager = userManager;
        _configuration = configuration;
        _outbox = outbox;
        _emailOptions = emailOptions;
        _emailChecker = emailChecker;
    }

    // POST: api/auth/login
    /// <summary>Sign in with email and password. Copy accessToken from the response and click Authorize.</summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 401)]
    [ProducesResponseType(typeof(ApiResultDto), 429)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
            return Unauthorized(ApiResultDto.Fail("Email or password is incorrect."));

        // Çoxlu səhv cəhd -> hesab müvəqqəti bloklanıb
        if (await _userManager.IsLockedOutAsync(user))
            return Unauthorized(ApiResultDto.Fail(LockedMessage(await _userManager.GetLockoutEndDateAsync(user))));

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);

            if (await _userManager.IsLockedOutAsync(user))
                return Unauthorized(ApiResultDto.Fail(LockedMessage(await _userManager.GetLockoutEndDateAsync(user))));

            var left = _userManager.Options.Lockout.MaxFailedAccessAttempts - await _userManager.GetAccessFailedCountAsync(user);

            return Unauthorized(ApiResultDto.Fail(left <= 2
                ? $"Email or password is incorrect. {left} attempt{(left == 1 ? "" : "s")} left before the account is locked for a few minutes."
                : "Email or password is incorrect."));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return Ok(await CreateTokenAsync(user));
    }

    // POST: api/auth/register
    /// <summary>Create a new user account and sign in.</summary>
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        // E-poçt real olmalıdır: format, birdəfəlik (bot) domen, poçt serveri (MX)
        var check = await _emailChecker.CheckAsync(request.Email, HttpContext.RequestAborted);

        if (!check.IsValid)
        {
            return BadRequest(ApiResultDto.Fail(check.Suggestion is null
                ? check.Reason!
                : $"{check.Reason} Did you mean {check.Suggestion}?"));
        }

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

        // Təsdiq məktubu: link ilə e-poçtun həqiqətən bu istifadəçiyə aid olduğu yoxlanır
        await SendConfirmationEmailAsync(user);

        return Ok(await CreateTokenAsync(user));
    }

    // GET: api/auth/check-email?email=...
    /// <summary>Check an email before registration: format, disposable (temporary) domain, typo and mail server (MX).</summary>
    [HttpGet("check-email")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(EmailCheckResult), 200)]
    public async Task<IActionResult> CheckEmail([FromQuery] string email)
        => Ok(await _emailChecker.CheckAsync(email ?? string.Empty, HttpContext.RequestAborted));

    // POST: api/auth/confirm-email
    /// <summary>Confirm the email address with the token from the welcome email.</summary>
    [HttpPost("confirm-email")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
    {
        const string invalid = "This confirmation link is invalid or has expired. Please request a new one from your profile.";

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());

        if (user == null)
            return BadRequest(ApiResultDto.Fail(invalid));

        if (user.EmailConfirmed)
            return Ok(ApiResultDto.Ok("Your email is already confirmed."));

        string token;

        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));
        }
        catch (FormatException)
        {
            return BadRequest(ApiResultDto.Fail(invalid));
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);

        return result.Succeeded
            ? Ok(ApiResultDto.Ok("Thank you! Your email has been confirmed."))
            : BadRequest(ApiResultDto.Fail(invalid));
    }

    // POST: api/auth/resend-confirmation
    /// <summary>Send the confirmation email again (signed-in user).</summary>
    [HttpPost("resend-confirmation")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    public async Task<IActionResult> ResendConfirmation()
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId ?? string.Empty);

        if (user == null)
            return Unauthorized(ApiResultDto.Fail("User not found."));

        if (user.EmailConfirmed)
            return Ok(ApiResultDto.Ok("Your email is already confirmed."));

        await SendConfirmationEmailAsync(user);

        return Ok(ApiResultDto.Ok("We have sent a new confirmation link to your email."));
    }

    // POST: api/auth/forgot-password
    /// <summary>Send a password reset link by email. Always returns OK, so nobody can find out which emails are registered.</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        const string message = "If an account exists for this email, a reset link has been sent.";

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());

        if (user == null)
            return Ok(ApiResultDto.Ok(message));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var link = AccountEmailTemplates.ResetLink(_emailOptions.PublicSiteUrl, user.Email!, encoded);
        var (subject, html) = AccountEmailTemplates.PasswordReset(user.FirstName, link);

        await _outbox.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = subject,
            HtmlBody = html,
            LogHtmlBody = AccountEmailTemplates.PasswordResetForLog(user.FirstName).Html
        }, AccountEmailTemplates.PasswordResetType, null);

        return Ok(ApiResultDto.Ok(message));
    }

    // POST: api/auth/reset-password
    /// <summary>Set a new password with the token from the reset email. Also unlocks a locked account.</summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        const string invalid = "This reset link is invalid or has expired. Please request a new one.";

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());

        if (user == null)
            return BadRequest(ApiResultDto.Fail(invalid));

        string token;

        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));
        }
        catch (FormatException)
        {
            return BadRequest(ApiResultDto.Fail(invalid));
        }

        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded)
        {
            var message = result.Errors.Any(e => e.Code == "InvalidToken")
                ? invalid
                : string.Join(" ", result.Errors.Select(e => e.Description));

            return BadRequest(ApiResultDto.Fail(message));
        }

        // Yeni şifrə qoyulandan sonra blok açılır
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return Ok(ApiResultDto.Ok("Your password has been changed. You can sign in now."));
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
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToList()
        });
    }

    private async Task SendConfirmationEmailAsync(AppUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var link = AccountEmailTemplates.ConfirmLink(_emailOptions.PublicSiteUrl, user.Email!, encoded);
        var (subject, html) = AccountEmailTemplates.EmailConfirmation(user.FirstName, link);

        await _outbox.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = subject,
            HtmlBody = html,
            LogHtmlBody = AccountEmailTemplates.EmailConfirmationForLog(user.FirstName).Html
        }, AccountEmailTemplates.EmailConfirmationType, null);
    }


    private static string LockedMessage(DateTimeOffset? lockoutEnd)
    {
        var minutes = lockoutEnd.HasValue
            ? Math.Max(1, (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes))
            : 5;

        return $"Too many failed attempts. The account is locked for {minutes} minute{(minutes == 1 ? "" : "s")}. " +
               "You can also reset your password.";
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
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToList()
        };
    }
}
