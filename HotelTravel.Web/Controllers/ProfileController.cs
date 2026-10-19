using HotelTravel.Application.DTOs.Profile;
using HotelTravel.Web.Services;
using HotelTravel.Web.Services.Api;
using HotelTravel.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

// İstifadəçi profili: məlumatlar, statistika, şifrə dəyişmə (hamısı API ilə)
[Authorize]
public class ProfileController : Controller
{
    private readonly IProfileApiClient _profileApi;

    public ProfileController(IProfileApiClient profileApi)
    {
        _profileApi = profileApi;
    }

    // GET: /Profile
    [HttpGet]
    public async Task<IActionResult> Index(string? tab = null)
    {
        var model = await BuildModelAsync();

        model.Profile = new UpdateProfileDto
        {
            FirstName = model.User.FirstName,
            LastName = model.User.LastName,
            PhoneNumber = model.User.PhoneNumber
        };

        model.ActiveTab = tab == "security" ? "security" : "details";

        return View(model);
    }

    // POST: /Profile/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([Bind(Prefix = "Profile")] UpdateProfileDto dto)
    {
        // Şifrə formunun sahələri bu formda yoxdur
        RemoveErrors("Password");

        if (!ModelState.IsValid)
            return await RedisplayAsync(dto, null, "details");

        var result = await _profileApi.UpdateAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await RedisplayAsync(dto, null, "details");
        }

        // Cookie-dəki ad, telefon dərhal yenilənsin (header-də görünür)
        await RefreshCookieAsync();

        TempData["ProfileSuccess"] = "Your profile has been updated.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Profile/Avatar  (boş dəyər -> baş hərflər)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Avatar(string? avatarKey)
    {
        var result = await _profileApi.UpdateAvatarAsync(
            string.IsNullOrWhiteSpace(avatarKey) ? null : avatarKey);

        if (!result.Success)
        {
            TempData["ProfileError"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        await RefreshCookieAsync();

        TempData["ProfileSuccess"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    // POST: /Profile/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordDto dto)
    {
        RemoveErrors("Profile");

        if (!ModelState.IsValid)
            return await RedisplayAsync(null, dto, "security");

        var result = await _profileApi.ChangePasswordAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await RedisplayAsync(null, dto, "security");
        }

        TempData["ProfileSuccess"] = "Your password has been changed.";

        return RedirectToAction(nameof(Index), new { tab = "security" });
    }


    // =========================
    // HELPERS
    // =========================
    private async Task<ProfileViewModel> BuildModelAsync()
    {
        var me = await _profileApi.GetMeAsync();
        var summary = await _profileApi.GetSummaryAsync();

        return new ProfileViewModel
        {
            User = me,
            Summary = summary
        };
    }

    private async Task<IActionResult> RedisplayAsync(
        UpdateProfileDto? profile,
        ChangePasswordDto? password,
        string tab)
    {
        var model = await BuildModelAsync();

        model.Profile = profile ?? new UpdateProfileDto
        {
            FirstName = model.User.FirstName,
            LastName = model.User.LastName,
            PhoneNumber = model.User.PhoneNumber
        };

        // Şifrələr təhlükəsizlik üçün yenidən göstərilmir
        model.Password = new ChangePasswordDto();
        model.ActiveTab = tab;

        return View(nameof(Index), model);
    }

    private void RemoveErrors(string prefix)
    {
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith(prefix + ".")).ToList())
            ModelState.Remove(key);
    }

    // API-dən yeni məlumatı alıb cookie-ni eyni token ilə yenidən yaradırıq
    private async Task RefreshCookieAsync()
    {
        var current = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var token = User.FindFirst(AuthClaimTypes.AccessToken)?.Value;

        if (!current.Succeeded || string.IsNullOrEmpty(token))
            return;

        var me = await _profileApi.GetMeAsync();
        me.AccessToken = token;

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AuthCookie.BuildPrincipal(me),
            current.Properties ?? new AuthenticationProperties());
    }
}
