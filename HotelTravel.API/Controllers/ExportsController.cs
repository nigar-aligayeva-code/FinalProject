using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

/// <summary>Admin reports: download bookings, flights, taxis or car rentals as Excel (.xlsx) or CSV.</summary>
[Route("api/exports")]
[Authorize(Roles = "Admin")]
public class ExportsController : ApiControllerBase
{
    private readonly IExportService _exportService;

    public ExportsController(IExportService exportService)
    {
        _exportService = exportService;
    }

    // GET: api/exports/bookings?format=xlsx
    /// <summary>Download a report. type: bookings, flights, taxis or cars. format: xlsx (default) or csv (admin).</summary>
    [HttpGet("{type}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ApiResultDto), 404)]
    public async Task<IActionResult> Export(string type, [FromQuery] string format = "xlsx")
    {
        var file = await _exportService.ExportAsync(type, format);

        if (file == null)
            return NotFound(ApiResultDto.Fail("Unknown report. Use bookings, flights, taxis or cars."));

        return File(file.Content, file.ContentType, file.FileName);
    }
}
