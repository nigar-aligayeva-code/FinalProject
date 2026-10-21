using HotelTravel.Application.DTOs.Account;
using HotelTravel.Application.DTOs.Auth;
using HotelTravel.Web.Services;
using HotelTravel.Web.Services.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

// Login / register API üzərindən (JWT) işləyir
public class AccountController : Controller
{
    private readonly IAuthApiClient _authApi;

    public AccountController(IAuthApiClient authApi)
    {
        _authApi = authApi;
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

        // Register-dən sonra avtomatik login
        await SignInAsync(auth, isPersistent: false);

        return RedirectToAction("Index", "Home");
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
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var result = await _authApi.ForgotPasswordAsync(dto.Email);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(dto);
        }

        // Email mövcud olsa da, olmasa da eyni mesaj (kimin qeydiyyatda olduğu bilinməsin)
        TempData["ResetSent"] = result.Message;

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
