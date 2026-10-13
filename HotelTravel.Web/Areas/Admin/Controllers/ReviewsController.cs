using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // ALL REVIEWS
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int? rating)
        {
            var reviews = await _reviewService.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                reviews = reviews.Where(r =>
                    r.ConfirmationCode.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) ||
                    r.GuestDisplayName.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) ||
                    r.RoomName.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) ||
                    r.RoomNumber.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) ||
                    r.Comment.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (rating.HasValue)
            {
                reviews = reviews
                    .Where(r => r.Rating == rating.Value)
                    .ToList();
            }

            ViewBag.Search = search;
            ViewBag.Rating = rating;

            return View(reviews);
        }

        // DELETE (SOFT)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _reviewService.DeleteAsync(id);

            TempData[result ? "SuccessMessage" : "ErrorMessage"] =
                result
                    ? "Review deleted successfully."
                    : "Review was not found.";

            return RedirectToAction(nameof(Index));
        }
    }
}
