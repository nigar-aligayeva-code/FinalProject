using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    // Göndərilən məktublar (API: GET api/emails)
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmailsController : Controller
    {
        private readonly IEmailLogService _emailLogService;

        public EmailsController(IEmailLogService emailLogService)
        {
            _emailLogService = emailLogService;
        }

        public async Task<IActionResult> Index(int? id)
        {
            var emails = await _emailLogService.GetRecentAsync(100);

            // Seçilmiş (yoxdursa ən sonuncu) məktubun HTML mətni
            var selectedId = id ?? emails.FirstOrDefault()?.Id;

            ViewBag.Selected = selectedId.HasValue
                ? await _emailLogService.GetByIdAsync(selectedId.Value)
                : null;

            return View(emails);
        }
    }
}
