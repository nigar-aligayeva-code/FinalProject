using HotelTravel.Application.DTOs.Analytics;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

// Admin analitikası (gəlir, doluluq, top şəhərlər, reytinqlər)
/// <summary>Business analytics for the admin dashboard.</summary>
[Route("api/analytics")]
[Authorize(Roles = "Admin")]
public class AnalyticsController : ApiControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    // GET: api/analytics
    /// <summary>Revenue by service and month, occupancy forecast, cancellation rate, top cities, top rooms and ratings (admin).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AnalyticsDto), 200)]
    public async Task<IActionResult> Get()
        => Ok(await _analyticsService.GetAsync());
}
