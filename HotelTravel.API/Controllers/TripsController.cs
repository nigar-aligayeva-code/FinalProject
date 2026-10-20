using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Trips;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

// Trip paketi: uçuş + otel + taksi bir sifarişdə
[Route("api/trips")]
public class TripsController : ApiControllerBase
{
    private readonly ITripPackageService _tripService;
    private readonly IBookingEmailService _bookingEmails;
    private readonly IActivityNotifier _notifier;

    public TripsController(
        ITripPackageService tripService,
        IBookingEmailService bookingEmails,
        IActivityNotifier notifier)
    {
        _tripService = tripService;
        _bookingEmails = bookingEmails;
        _notifier = notifier;
    }

    // GET: api/trips/origins
    [HttpGet("origins")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> Origins()
        => Ok(await _tripService.GetOriginsAsync());

    // GET: api/trips/destinations
    [HttpGet("destinations")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> Destinations()
        => Ok(await _tripService.GetDestinationsAsync());

    // POST: api/trips/plan  (uçuşlar, otaqlar, taksi təklifi)
    [HttpPost("plan")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TripPlanDto), 200)]
    public async Task<IActionResult> Plan([FromBody] TripSearchDto search)
        => Ok(await _tripService.PlanAsync(search));

    // POST: api/trips
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> Book([FromBody] CreateTripPackageDto dto)
    {
        var result = await _tripService.BookAsync(dto, CurrentUserId!);

        if (!result.Success)
            return BadRequest(ApiResultDto.Fail(result.Message));

        // Otel təsdiq məktubu + PDF voucher
        var package = await _tripService.GetByCodeAsync(result.Code!, CurrentUserId!);

        if (package != null)
            await _bookingEmails.SendConfirmationAsync(package.HotelBookingCode);

        await _notifier.TripBookedAsync(result.Code!);

        return Ok(ApiResultDto.Ok(result.Message, code: result.Code));
    }

    // GET: api/trips/mine
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<TripPackageDto>), 200)]
    public async Task<IActionResult> Mine()
        => Ok(await _tripService.GetByUserAsync(CurrentUserId!));

    // GET: api/trips/TP-1234ABCD
    [HttpGet("{code}")]
    [Authorize]
    [ProducesResponseType(typeof(TripPackageDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 404)]
    public async Task<IActionResult> GetByCode(string code)
        => OkOrNotFound(await _tripService.GetByCodeAsync(code, CurrentUserId!), "Trip package not found.");
}
