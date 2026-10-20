using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

[Route("api/flights")]
public class FlightsController : ApiControllerBase
{
    private readonly IFlightService _flightService;
    private readonly IActivityNotifier _notifier;

    public FlightsController(IFlightService flightService, IActivityNotifier notifier)
    {
        _flightService = flightService;
        _notifier = notifier;
    }

    // =========================
    // PUBLIC
    // =========================

    // GET: api/flights/cities
    [HttpGet("cities")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> Cities()
        => Ok(await _flightService.GetCitiesAsync());

    // GET: api/flights/search?fromCity=Baku&toCity=Istanbul&date=2026-11-01&passengers=1&cabin=Economy
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<FlightOptionDto>), 200)]
    public async Task<IActionResult> Search(
        [FromQuery] string fromCity,
        [FromQuery] string toCity,
        [FromQuery] DateTime date,
        [FromQuery] int passengers = 1,
        [FromQuery] CabinClass cabin = CabinClass.Economy)
        => Ok(await _flightService.SearchAsync(fromCity, toCity, date, passengers, cabin));

    // GET: api/flights/day-fares?fromCity=...&toCity=...&date=...&cabin=...
    [HttpGet("day-fares")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<FlightDayFareDto>), 200)]
    public async Task<IActionResult> DayFares(
        [FromQuery] string fromCity,
        [FromQuery] string toCity,
        [FromQuery] DateTime date,
        [FromQuery] CabinClass cabin = CabinClass.Economy)
        => Ok(await _flightService.GetDayFaresAsync(fromCity, toCity, date, cabin));

    // GET: api/flights/options/5?date=...&passengers=1&cabin=Economy
    [HttpGet("options/{routeId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FlightOptionDto), 200)]
    public async Task<IActionResult> Option(
        int routeId,
        [FromQuery] DateTime date,
        [FromQuery] int passengers = 1,
        [FromQuery] CabinClass cabin = CabinClass.Economy)
        => OkOrNotFound(
            await _flightService.GetOptionAsync(routeId, date, passengers, cabin),
            "Flight not found.");

    // POST: api/flights/bookings
    [HttpPost("bookings")]
    [AllowAnonymous]
    public async Task<IActionResult> Book([FromBody] CreateFlightBookingDto dto)
    {
        var result = await _flightService.BookAsync(dto, CurrentUserId);

        if (result.Success && result.ConfirmationCode != null)
            await _notifier.FlightBookedAsync(result.ConfirmationCode);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message, code: result.ConfirmationCode)
            : ApiResultDto.Fail(result.Message));
    }

    // GET: api/flights/bookings/by-code/FL-1234ABCD
    [HttpGet("bookings/by-code/{confirmationCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FlightBookingDto), 200)]
    public async Task<IActionResult> GetByCode(string confirmationCode)
        => OkOrNotFound(await _flightService.GetByCodeAsync(confirmationCode), "Flight booking not found.");

    // GET: api/flights/bookings/mine
    [HttpGet("bookings/mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<FlightBookingDto>), 200)]
    public async Task<IActionResult> Mine()
        => Ok(await _flightService.GetByUserAsync(CurrentUserId!));

    // POST: api/flights/bookings/5/cancel-mine
    [HttpPost("bookings/{id:int}/cancel-mine")]
    [Authorize]
    public async Task<IActionResult> CancelMine(int id)
    {
        var result = await _flightService.CancelByUserAsync(id, CurrentUserId!);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // =========================
    // ADMIN
    // =========================

    // GET: api/flights/routes?city=Baku
    [HttpGet("routes")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<FlightRouteDto>), 200)]
    public async Task<IActionResult> Routes([FromQuery] string? city)
        => Ok(await _flightService.GetRoutesAsync(city));

    // GET: api/flights/routes/5/edit
    [HttpGet("routes/{id:int}/edit")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SaveFlightRouteDto), 200)]
    public async Task<IActionResult> GetRouteForEdit(int id)
        => OkOrNotFound(await _flightService.GetRouteForEditAsync(id), "Flight not found.");

    // POST: api/flights/routes  (Id varsa yeniləyir)
    [HttpPost("routes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveRoute([FromBody] SaveFlightRouteDto dto)
    {
        var result = await _flightService.SaveRouteAsync(dto);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // POST: api/flights/routes/5/toggle
    [HttpPost("routes/{id:int}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleRoute(int id)
        => FromBool(await _flightService.ToggleRouteAsync(id), "Flight updated.", "Flight not found.");

    // GET: api/flights/bookings?status=Confirmed
    [HttpGet("bookings")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<FlightBookingDto>), 200)]
    public async Task<IActionResult> Bookings([FromQuery] FlightBookingStatus? status)
        => Ok(await _flightService.GetBookingsAsync(status));

    // PUT: api/flights/bookings/5/status
    [HttpPut("bookings/{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BookingStatus(
        int id,
        [FromBody] StatusUpdateDto<FlightBookingStatus> dto)
        => FromBool(
            await _flightService.SetBookingStatusAsync(id, dto.Status),
            "Flight booking updated.",
            "This flight booking cannot be changed.");
}
