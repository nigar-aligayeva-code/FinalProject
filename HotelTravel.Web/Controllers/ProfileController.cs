using HotelTravel.Application.DTOs.Profile;
using HotelTravel.Application.Services;
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
    private const long MaxAvatarBytes = 2 * 1024 * 1024; // 2 MB

    private readonly IProfileApiClient _profileApi;
    private readonly IWebHostEnvironment _environment;

    public ProfileController(IProfileApiClient profileApi, IWebHostEnvironment environment)
    {
        _profileApi = profileApi;
        _environment = environment;
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
        var previous = User.FindFirst(AuthClaimTypes.Avatar)?.Value;

        var result = await _profileApi.UpdateAvatarAsync(
            string.IsNullOrWhiteSpace(avatarKey) ? null : avatarKey);

        if (!result.Success)
        {
            TempData["ProfileError"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        if (previous != avatarKey)
            DeleteUploadedAvatar(previous);

        await RefreshCookieAsync();

        TempData["ProfileSuccess"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    // POST: /Profile/UploadAvatar  (öz şəklini avatar kimi qoymaq)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxAvatarBytes + 64 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile? photo)
    {
        if (photo is null || photo.Length == 0)
        {
            TempData["ProfileError"] = "Please choose a photo.";
            return RedirectToAction(nameof(Index));
        }

        if (photo.Length > MaxAvatarBytes)
        {
            TempData["ProfileError"] = "The photo must be 2 MB or smaller.";
            return RedirectToAction(nameof(Index));
        }

        // Uzantıya yox, faylın özünə (ilk baytlara) baxırıq: ".jpg" adlı başqa fayl keçməsin
        var extension = await DetectImageExtensionAsync(photo);

        if (extension is null)
        {
            TempData["ProfileError"] = "Only JPG, PNG or WEBP photos are allowed.";
            return RedirectToAction(nameof(Index));
        }

        var folder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension}";

        await using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.CreateNew))
        {
            await photo.CopyToAsync(stream);
        }

        var key = AvatarCatalog.UploadFolder + fileName;
        var previous = User.FindFirst(AuthClaimTypes.Avatar)?.Value;

        var result = await _profileApi.UpdateAvatarAsync(key);

        if (!result.Success)
        {
            DeleteUploadedAvatar(key);
            TempData["ProfileError"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // Köhnə yüklənmiş şəkil artıq lazım deyil
        if (previous != key)
            DeleteUploadedAvatar(previous);

        await RefreshCookieAsync();

        TempData["ProfileSuccess"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    // JPEG: FF D8 FF · PNG: 89 50 4E 47 · WEBP: "RIFF" .... "WEBP"
    private static async Task<string?> DetectImageExtensionAsync(IFormFile file)
    {
        var header = new byte[12];

        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header);

        if (read < 12)
            return null;

        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return ".jpg";

        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            return ".png";

        if (header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' &&
            header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P')
            return ".webp";

        return null;
    }

    private void DeleteUploadedAvatar(string? key)
    {
        // Yalnız bizim formatda olan fayl silinir (başqa yol qəbul olunmur)
        if (!AvatarCatalog.IsUpload(key))
            return;

        var path = Path.Combine(_environment.WebRootPath, "uploads", "avatars", Path.GetFileName(key!));

        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
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
