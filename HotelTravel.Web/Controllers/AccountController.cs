using HotelTravel.Application.DTOs.Account;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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

        var existingUser =
            await _userManager.FindByEmailAsync(dto.Email);

        if (existingUser != null)
        {
            ModelState.AddModelError(
                nameof(dto.Email),
                "An account with this email already exists.");

            return View(dto);
        }

        var user = new AppUser
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            UserName = dto.Email
        };

        var result =
            await _userManager.CreateAsync(
                user,
                dto.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(dto);
        }
        var roleResult =
    await _userManager.AddToRoleAsync(
        user,
        "User");

        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(dto);
        }

        // Register-dən sonra avtomatik login
        await _signInManager.SignInAsync(
            user,
            isPersistent: false);

        return RedirectToAction(
            "Index",
            "Home");
    }


    // LOGIN GET
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // LOGIN POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var user =
            await _userManager.FindByEmailAsync(dto.Email);

        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Email or password is incorrect.");

            return View(dto);
        }

        var result =
            await _signInManager.PasswordSignInAsync(
                user,
                dto.Password,
                dto.RememberMe,
                lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                string.Empty,
                "Email or password is incorrect.");

            return View(dto);
        }

        return RedirectToAction(
            "Index",
            "Home");
    }


    // LOGOUT
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction(
            "Index",
            "Home");
    }


    // ACCESS DENIED
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}