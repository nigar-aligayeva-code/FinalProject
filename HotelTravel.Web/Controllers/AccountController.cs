using HotelTravel.Application.DTOs.Account;
using HotelTravel.Application.DTOs.Auth;
using HotelTravel.Web.Services;
using HotelTravel.Web.Services.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HotelTravel.Web.Controllers;

// Login / register API üzərindən (JWT) işləyir
public class AccountController : Controller
{
    private readonly IAuthApiClient _authApi;
    private readonly IStringLocalizer<SharedResource> _t;

    public AccountController(IAuthApiClient authApi, IStringLocalizer<SharedResource> localizer)
    {
        _authApi = authApi;
        _t = localizer;
    }

    // REGISTER GET
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // REGISTER POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var (auth, error) = await _authApi.RegisterAsync(dto);

        if (auth == null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Registration failed.");
            return View(dto);
        }

        // Register-dən sonra avtomatik login; təsdiq məktubu göndərilib
        await SignInAsync(auth, isPersistent: false);

        TempData["ProfileSuccess"] = "Welcome! We have sent a confirmation link to your email.";

        return RedirectToAction("Index", "Profile");
    }

    // Qeydiyyat formu: e-poçtu yazan kimi yoxla (yazı səhvi, birdəfəlik domen, MX)
    [HttpGet]
    public async Task<IActionResult> CheckEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Json(new { isValid = false });

        var result = await _authApi.CheckEmailAsync(email)
            ?? new HotelTravel.Application.DTOs.Email.EmailCheckResult { IsValid = true }; // API yoxdursa forma işləyir

        return Json(new
        {
            result.IsValid,
            Reason = result.Reason is null ? null : _t[result.Reason].Value,
            result.Suggestion,
            DidYouMean = result.Suggestion is null ? null : _t["Did you mean {0}?", result.Suggestion].Value
        });
    }

    // E-POÇT TƏSDİQİ (məktubdakı link)
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Index", "Home");

        var result = await _authApi.ConfirmEmailAsync(new ConfirmEmailDto { Email = email, Token = token });

        ViewBag.Success = result.Success;
        ViewBag.Message = result.Message;

        return View();
    }


    // LOGIN GET
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // [Authorize] səhifədən gəlibsə, login-dən sonra ora qayıtsın
        ViewBag.ReturnUrl = returnUrl;

        return View();
    }

    // LOGIN POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(dto);

        var (auth, error) = await _authApi.LoginAsync(dto.Email, dto.Password);

        if (auth == null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Email or password is incorrect.");
            return View(dto);
        }

        await SignInAsync(auth, dto.RememberMe);

        // Yalnız öz saytımızdakı ünvana qayıt (open redirect olmasın)
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        // Admin birbaşa admin panelə
        if (auth.Roles.Contains("Admin"))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        // Otel işçisi birbaşa partnyor panelinə
        if (auth.Roles.Contains("HotelPartner"))
            return RedirectToAction("Index", "Dashboard", new { area = "Partner" });

        return RedirectToAction("Index", "Home");
    }


    // LOGOUT
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }


    // FORGOT PASSWORD
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto, [FromServices] BotGuard botGuard)
    {
        if (!ModelState.IsValid)
            return View(dto);

        // Bot (gizli sahəni doldurub və ya formu 3 saniyədən tez göndərib): məktub getmir,
        // amma cavab eynidir - bot yoxlandığını bilməsin
        if (!botGuard.IsHuman(Request.Form))
        {
            TempData["ResetSent"] = "If an account exists for this email, a reset link has been sent.";
            TempData["ResetEmail"] = HotelTravel.Application.Services.BotGuardRules.MaskEmail(dto.Email);
            return RedirectToAction(nameof(ForgotPassword));
        }

        var result = await _authApi.ForgotPasswordAsync(dto.Email);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(dto);
        }

        // Email mövcud olsa da, olmasa da eyni mesaj (kimin qeydiyyatda olduğu bilinməsin)
        TempData["ResetSent"] = result.Message;
        TempData["ResetEmail"] = HotelTravel.Application.Services.BotGuardRules.MaskEmail(dto.Email);

        return RedirectToAction(nameof(ForgotPassword));
    }


    // RESET PASSWORD (məktubdakı link)
    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(ForgotPassword));

        return View(new ResetPasswordDto { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var result = await _authApi.ResetPasswordAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(dto);
        }

        TempData["LoginMessage"] = result.Message;

        return RedirectToAction(nameof(Login));
    }


    // ACCESS DENIED
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }


    // API-dən gələn məlumatla cookie yaradılır; JWT token də cookie-də saxlanır
    private async Task SignInAsync(AuthResponseDto auth, bool isPersistent)
    {
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AuthCookie.BuildPrincipal(auth),
            new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                // Cookie token ilə eyni vaxtda bitsin
                ExpiresUtc = auth.ExpiresAtUtc
            });
    }
}
