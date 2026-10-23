using HotelTravel.Application.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

public class LanguageController : Controller
{
    // =========================
    // DİL SEÇİMİ: /Language/Set?culture=az&returnUrl=/Hotels
    // Seçim 1 il cookie-də saxlanılır.
    // =========================
    [HttpGet]
    public IActionResult Set(string? culture, string? returnUrl)
    {
        var language = LanguageRules.Normalize(culture);

        // Rəqəm/tarix formatı həmişə en-US, yalnız interfeys dili dəyişir
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en-US", language)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

        // Yalnız öz saytımıza qayıdırıq (open redirect olmasın)
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
