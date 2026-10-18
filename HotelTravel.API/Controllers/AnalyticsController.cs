using HotelTravel.Application.DTOs.Analytics;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

// Admin analitikası (gəlir, doluluq, top şəhərlər, reytinqlər)
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
    [HttpGet]
    [ProducesResponseType(typeof(AnalyticsDto), 200)]
    public async Task<IActionResult> Get()
        => Ok(await _analyticsService.GetAsync());
}
