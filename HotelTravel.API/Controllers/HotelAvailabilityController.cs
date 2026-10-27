using HotelTravel.API.Services;
using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

/// <summary>Real availability of a hotel: closed dates, room prices, "checked" time and request checks. Hotel partners manage only their own hotel, admins any hotel.</summary>
[Route("api/hotels/{hotelId:int}/availability")]
[Authorize(Roles = "Admin," + PartnerScope.PartnerRole)]
public class HotelAvailabilityController : ApiControllerBase
{
    private readonly IHotelAvailabilityService _availability;
    private readonly PartnerScope _scope;

    public HotelAvailabilityController(IHotelAvailabilityService availability, PartnerScope scope)
    {
        _availability = availability;
        _scope = scope;
    }

    // GET: api/hotels/1/availability
    /// <summary>Rooms, prices, closed dates and how fresh the availability data is.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(HotelAvailabilityDto), 200)]
    public async Task<IActionResult> Get(int hotelId)
    {
        if (!await _scope.CanManageHotelAsync(User, hotelId))
            return Forbid();

        return OkOrNotFound(await _availability.GetAsync(hotelId), "Hotel not found.");
    }

    // GET: api/hotels/1/availability/bookings?status=Pending
    /// <summary>Bookings of this hotel (pending requests first).</summary>
    [HttpGet("bookings")]
    [ProducesResponseType(typeof(List<GetBookingDto>), 200)]
    public async Task<IActionResult> Bookings(int hotelId, [FromQuery] BookingStatus? status)
    {
        if (!await _scope.CanManageHotelAsync(User, hotelId))
            return Forbid();

        return Ok(await _availability.GetBookingsAsync(hotelId, status));
    }

    // POST: api/hotels/1/availability/blocks
    /// <summary>Close dates for one room (or all rooms when RoomId is empty): sold elsewhere, renovation…</summary>
    [HttpPost("blocks")]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> AddBlock(int hotelId, [FromBody] CreateRoomBlockDto dto)
    {
        if (!await _scope.CanManageHotelAsync(User, hotelId))
            return Forbid();

        var result = await _availability.AddBlockAsync(hotelId, dto, CurrentUserId);

        return FromResult(result.Success ? ApiResultDto.Ok(result.Message) : ApiResultDto.Fail(result.Message));
    }

    // DELETE: api/hotels/1/availability/blocks/5
    /// <summary>Open closed dates again.</summary>
    [HttpDelete("blocks/{blockId:int}")]
    public async Task<IActionResult> RemoveBlock(int hotelId, int blockId)
    {
        if (!await _scope.CanManageHotelAsync(User, hotelId))
            return Forbid();

        var result = await _availability.RemoveBlockAsync(hotelId, blockId);

        return FromResult(result.Success ? ApiResultDto.Ok(result.Message) : ApiResultDto.Fail(result.Message));
    }

    // POST: api/hotels/1/availability/up-to-date
    /// <summary>"I have checked our real availability" — marks the hotel data as fresh.</summary>
    [HttpPost("up-to-date")]
    public async Task<IActionResult> MarkUpToDate(int hotelId)
    {
        if (!await _scope.CanManageHotelAsync(User, hotelId))
            return Forbid();

        return FromBool(
            await _availability.MarkUpToDateAsync(hotelId),
            "Availability marked as up to date.",
            "Hotel not found.");
    }

    // PUT: api/hotels/1/availability/rooms/5/price
    /// <summary>Change the nightly price of a room.</summary>
    [HttpPut("rooms/{roomId:int}/price")]
    public async Task<IActionResult> UpdatePrice(int hotelId, int roomId, [FromBody] UpdateRoomPriceDto dto)
    {
        if (!await _scope.CanManageHotelAsync(User, hotelId))
            return Forbid();

        var result = await _availability.UpdateRoomPriceAsync(hotelId, roomId, dto.PricePerNight);

        return FromResult(result.Success ? ApiResultDto.Ok(result.Message) : ApiResultDto.Fail(result.Message));
    }

    // GET: api/availability/check/5
    /// <summary>Before confirming a pending request: overlapping bookings, closed dates, data freshness.</summary>
    [HttpGet("~/api/availability/check/{bookingId:int}")]
    [ProducesResponseType(typeof(AvailabilityCheckDto), 200)]
    public async Task<IActionResult> Check(int bookingId)
    {
        var check = await _availability.CheckBookingAsync(bookingId);

        if (check is null)
            return NotFound(ApiResultDto.Fail("Booking not found."));

        if (!await _scope.CanManageHotelAsync(User, check.HotelId))
            return Forbid();

        return Ok(check);
    }
}
