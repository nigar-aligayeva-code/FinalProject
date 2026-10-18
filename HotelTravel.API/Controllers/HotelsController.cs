using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.DTOs.Hotels;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

[Route("api/hotels")]
public class HotelsController : ApiControllerBase
{
    private readonly IHotelService _hotelService;

    public HotelsController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    // GET: api/hotels
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GetHotelDto>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _hotelService.GetAllAsync());

    // GET: api/hotels/5
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetHotelDto), 200)]
    public async Task<IActionResult> GetById(int id)
        => OkOrNotFound(await _hotelService.GetByIdAsync(id), "Hotel not found.");

    // GET: api/hotels/destinations?count=6
    [HttpGet("destinations")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<DestinationDto>), 200)]
    public async Task<IActionResult> Destinations([FromQuery] int count = 6)
        => Ok(await _hotelService.GetPopularDestinationsAsync(count));

    // POST: api/hotels
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateHotelDto dto)
    {
        await _hotelService.CreateAsync(dto);
        return Ok(ApiResultDto.Ok("Hotel created."));
    }

    // PUT: api/hotels/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHotelDto dto)
        => FromBool(await _hotelService.UpdateAsync(id, dto), "Hotel updated.", "Hotel not found.");

    // DELETE: api/hotels/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
        => FromBool(await _hotelService.DeleteAsync(id), "Hotel deleted.", "Hotel not found.");

    // GET: api/hotels/5/upcoming-bookings
    [HttpGet("{id:int}/upcoming-bookings")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> UpcomingBookings(int id)
        => Ok(await _hotelService.CountUpcomingBookingsAsync(id));
}
