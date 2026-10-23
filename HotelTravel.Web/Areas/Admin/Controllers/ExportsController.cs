using HotelTravel.Web.Services.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers
{
    // Excel / CSV yükləmə (fayl API-də yaradılır)
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ExportsController : Controller
    {
        private static readonly string[] Types = { "bookings", "flights", "taxis", "cars" };

        private readonly IExportApiClient _exportApi;

        public ExportsController(IExportApiClient exportApi)
        {
            _exportApi = exportApi;
        }

        // GET: /Admin/Exports/Download?type=bookings&format=xlsx
        [HttpGet]
        public async Task<IActionResult> Download(string type, string format = "xlsx")
        {
            type = (type ?? string.Empty).Trim().ToLowerInvariant();
            var isCsv = string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase);

            if (!Types.Contains(type))
                return NotFound();

            var content = await _exportApi.DownloadAsync(type, isCsv ? "csv" : "xlsx");

            if (content == null)
                return NotFound();

            var fileName = $"Tripio-{type}-{DateTime.Now:yyyy-MM-dd}.{(isCsv ? "csv" : "xlsx")}";
            var contentType = isCsv
                ? "text/csv"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(content, contentType, fileName);
        }
    }
}
