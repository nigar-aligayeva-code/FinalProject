using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    // Analitika səhifəsi: bütün rəqəmlər API-dən gəlir (GET api/analytics)
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : Controller
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _analyticsService.GetAsync();

            return View(model);
        }
    }
}
