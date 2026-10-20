using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Taxis;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

/// <summary>Airport taxi transfers: hotels with airports, vehicle options, booking and admin management.</summary>
[Route("api/taxis")]
public class TaxisController : ApiControllerBase
{
    private readonly ITaxiService _taxiService;
    private readonly IActivityNotifier _notifier;

    public TaxisController(ITaxiService taxiService, IActivityNotifier notifier)
    {
        _taxiService = taxiService;
        _notifier = notifier;
    }

    // GET: api/taxis/hotels
    /// <summary>Hotels that have an airport taxi (with airport and distance).</summary>
    [HttpGet("hotels")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TaxiHotelDto>), 200)]
    public async Task<IActionResult> Hotels()
        => Ok(await _taxiService.GetHotelsAsync());

    // GET: api/taxis/hotels/5
    /// <summary>One hotel with its airport and distance.</summary>
    [HttpGet("hotels/{hotelId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TaxiHotelDto), 200)]
    public async Task<IActionResult> Hotel(int hotelId)
        => OkOrNotFound(await _taxiService.GetHotelAsync(hotelId), "Hotel not found.");

    // GET: api/taxis/options?distanceKm=25&passengers=2&luggage=2
    /// <summary>Vehicle classes and prices for a distance, passengers and luggage.</summary>
    [HttpGet("options")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TaxiOptionDto>), 200)]
    public IActionResult Options(
        [FromQuery] int distanceKm,
        [FromQuery] int passengers = 2,
        [FromQuery] int luggage = 2)
        => Ok(_taxiService.GetOptions(distanceKm, passengers, luggage));

    // POST: api/taxis/bookings
    /// <summary>Book an airport taxi.</summary>
    [HttpPost("bookings")]
    [AllowAnonymous]
    public async Task<IActionResult> Book([FromBody] CreateTaxiBookingDto dto)
    {
        var result = await _taxiService.BookAsync(dto, CurrentUserId);

        if (result.Success && result.ConfirmationCode != null)
            await _notifier.TaxiBookedAsync(result.ConfirmationCode);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message, code: result.ConfirmationCode)
            : ApiResultDto.Fail(result.Message));
    }

    // GET: api/taxis/bookings/by-code/TX-1234ABCD
    /// <summary>Find a taxi booking by its confirmation code.</summary>
    [HttpGet("bookings/by-code/{confirmationCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TaxiBookingDto), 200)]
    public async Task<IActionResult> GetByCode(string confirmationCode)
        => OkOrNotFound(await _taxiService.GetByCodeAsync(confirmationCode), "Taxi booking not found.");

    // GET: api/taxis/bookings/mine
    /// <summary>Taxi bookings of the signed-in user.</summary>
    [HttpGet("bookings/mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<TaxiBookingDto>), 200)]
    public async Task<IActionResult> Mine()
        => Ok(await _taxiService.GetByUserAsync(CurrentUserId!));

    // POST: api/taxis/bookings/5/cancel-mine
    /// <summary>Cancel one of the signed-in user's taxi bookings.</summary>
    [HttpPost("bookings/{id:int}/cancel-mine")]
    [Authorize]
    public async Task<IActionResult> CancelMine(int id)
    {
        var result = await _taxiService.CancelByUserAsync(id, CurrentUserId!);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // GET: api/taxis/bookings?status=Confirmed  (admin)
    /// <summary>All taxi bookings, optionally filtered by status (admin).</summary>
    [HttpGet("bookings")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<TaxiBookingDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] TaxiBookingStatus? status)
        => Ok(await _taxiService.GetAllAsync(status));

    // PUT: api/taxis/bookings/5/status
    /// <summary>Change taxi booking status (admin).</summary>
    [HttpPut("bookings/{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStatus(
        int id,
        [FromBody] StatusUpdateDto<TaxiBookingStatus> dto)
        => FromBool(
            await _taxiService.SetStatusAsync(id, dto.Status),
            "Taxi booking updated.",
            "This taxi booking cannot be changed.");
}
