using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.DTOs.Hotels;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

/// <summary>Hotels: catalogue, popular destinations and admin management.</summary>
[Route("api/hotels")]
public class HotelsController : ApiControllerBase
{
    private readonly IHotelService _hotelService;

    public HotelsController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    // GET: api/hotels
    /// <summary>List all hotels.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GetHotelDto>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _hotelService.GetAllAsync());

    // GET: api/hotels/5
    /// <summary>One hotel with its photo gallery.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetHotelDto), 200)]
    public async Task<IActionResult> GetById(int id)
        => OkOrNotFound(await _hotelService.GetByIdAsync(id), "Hotel not found.");

    // GET: api/hotels/destinations?count=6
    /// <summary>Popular destinations (cities with hotel count and lowest price).</summary>
    [HttpGet("destinations")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<DestinationDto>), 200)]
    public async Task<IActionResult> Destinations([FromQuery] int count = 6)
        => Ok(await _hotelService.GetPopularDestinationsAsync(count));

    // POST: api/hotels
    /// <summary>Create a hotel (admin).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateHotelDto dto,
        [FromServices] IFlightNetworkService flightNetwork)
    {
        await _hotelService.CreateAsync(dto);

        // Yeni şəhərdə otel: reyslər avtomatik yaranır (admin reys yaratmır)
        await flightNetwork.GenerateMissingAsync(HttpContext.RequestAborted);

        return Ok(ApiResultDto.Ok("Hotel created."));
    }

    // PUT: api/hotels/5
    /// <summary>Update a hotel (admin).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateHotelDto dto,
        [FromServices] IFlightNetworkService flightNetwork)
    {
        var updated = await _hotelService.UpdateAsync(id, dto);

        // Şəhər dəyişibsə, yeni şəhərin reysləri də avtomatik yaransın
        if (updated)
            await flightNetwork.GenerateMissingAsync(HttpContext.RequestAborted);

        return FromBool(updated, "Hotel updated.", "Hotel not found.");
    }

    // DELETE: api/hotels/5
    /// <summary>Delete a hotel (soft delete). Not allowed while it has upcoming bookings (admin).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
        => FromBool(await _hotelService.DeleteAsync(id), "Hotel deleted.", "Hotel not found.");

    // GET: api/hotels/5/upcoming-bookings
    /// <summary>Number of upcoming bookings of a hotel (admin).</summary>
    [HttpGet("{id:int}/upcoming-bookings")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> UpcomingBookings(int id)
        => Ok(await _hotelService.CountUpcomingBookingsAsync(id));
}
