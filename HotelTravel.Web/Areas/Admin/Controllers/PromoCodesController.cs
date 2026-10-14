using HotelTravel.Application.DTOs.PromoCodes;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PromoCodesController : Controller
    {
        private readonly IPromoCodeService _promoCodeService;

        public PromoCodesController(IPromoCodeService promoCodeService)
        {
            _promoCodeService = promoCodeService;
        }

        // LIST + CREATE FORM
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.PromoCodes = await _promoCodeService.GetAllAsync();

            return View(new CreatePromoCodeDto());
        }

        // CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePromoCodeDto dto)
        {
            if (ModelState.IsValid)
            {
                var result = await _promoCodeService.CreateAsync(dto);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, result.Message);
            }

            ViewBag.PromoCodes = await _promoCodeService.GetAllAsync();

            return View("Index", dto);
        }

        // ACTIVATE / DEACTIVATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var ok = await _promoCodeService.ToggleActiveAsync(id);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Promo code updated." : "Promo code was not found.";

            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _promoCodeService.DeleteAsync(id);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Promo code deleted." : "Promo code was not found.";

            return RedirectToAction(nameof(Index));
        }
    }
}
