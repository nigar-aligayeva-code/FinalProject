using HotelTravel.Application.DTOs.Reviews;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }


    // =========================
    // PUBLIC - WRITE REVIEW (FORM)
    // =========================
    [HttpGet]
    public async Task<IActionResult> Create(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return RedirectToAction("Find", "Bookings");

        var eligibility =
            await _reviewService.CheckEligibilityAsync(confirmationCode);

        if (!eligibility.CanReview)
        {
            TempData["ErrorMessage"] = eligibility.Message;

            return RedirectToAction(
                "Find",
                "Bookings",
                new { confirmationCode });
        }

        ViewBag.Eligibility = eligibility;

        var dto = new CreateReviewDto
        {
            ConfirmationCode = eligibility.ConfirmationCode
        };

        return View(dto);
    }


    // =========================
    // PUBLIC - WRITE REVIEW (SUBMIT)
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewDto dto)
    {
        if (!ModelState.IsValid)
        {
            return await ReturnFormAsync(dto);
        }

        var result = await _reviewService.CreateAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            return await ReturnFormAsync(dto);
        }

        TempData["SuccessMessage"] = result.Message;

        return RedirectToAction(
            "Details",
            "Rooms",
            new { id = result.RoomId });
    }


    // =========================
    // PRIVATE HELPER
    // =========================
    private async Task<IActionResult> ReturnFormAsync(CreateReviewDto dto)
    {
        var eligibility =
            await _reviewService.CheckEligibilityAsync(dto.ConfirmationCode);

        if (eligibility.RoomId == 0)
        {
            // Rezervasiya ümumiyyətlə tapılmadı
            TempData["ErrorMessage"] = eligibility.Message;
            return RedirectToAction("Find", "Bookings");
        }

        ViewBag.Eligibility = eligibility;

        return View("Create", dto);
    }
}
