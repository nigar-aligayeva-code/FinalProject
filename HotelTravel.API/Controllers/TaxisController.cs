using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Taxis;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

[Route("api/taxis")]
public class TaxisController : ApiControllerBase
{
    private readonly ITaxiService _taxiService;

    public TaxisController(ITaxiService taxiService)
    {
        _taxiService = taxiService;
    }

    // GET: api/taxis/hotels
    [HttpGet("hotels")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TaxiHotelDto>), 200)]
    public async Task<IActionResult> Hotels()
        => Ok(await _taxiService.GetHotelsAsync());

    // GET: api/taxis/hotels/5
    [HttpGet("hotels/{hotelId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TaxiHotelDto), 200)]
    public async Task<IActionResult> Hotel(int hotelId)
        => OkOrNotFound(await _taxiService.GetHotelAsync(hotelId), "Hotel not found.");

    // GET: api/taxis/options?distanceKm=25&passengers=2&luggage=2
    [HttpGet("options")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TaxiOptionDto>), 200)]
    public IActionResult Options(
        [FromQuery] int distanceKm,
        [FromQuery] int passengers = 2,
        [FromQuery] int luggage = 2)
        => Ok(_taxiService.GetOptions(distanceKm, passengers, luggage));

    // POST: api/taxis/bookings
    [HttpPost("bookings")]
    [AllowAnonymous]
    public async Task<IActionResult> Book([FromBody] CreateTaxiBookingDto dto)
    {
        var result = await _taxiService.BookAsync(dto, CurrentUserId);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message, code: result.ConfirmationCode)
            : ApiResultDto.Fail(result.Message));
    }

    // GET: api/taxis/bookings/by-code/TX-1234ABCD
    [HttpGet("bookings/by-code/{confirmationCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TaxiBookingDto), 200)]
    public async Task<IActionResult> GetByCode(string confirmationCode)
        => OkOrNotFound(await _taxiService.GetByCodeAsync(confirmationCode), "Taxi booking not found.");

    // GET: api/taxis/bookings/mine
    [HttpGet("bookings/mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<TaxiBookingDto>), 200)]
    public async Task<IActionResult> Mine()
        => Ok(await _taxiService.GetByUserAsync(CurrentUserId!));

    // POST: api/taxis/bookings/5/cancel-mine
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
    [HttpGet("bookings")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<TaxiBookingDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] TaxiBookingStatus? status)
        => Ok(await _taxiService.GetAllAsync(status));

    // PUT: api/taxis/bookings/5/status
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
