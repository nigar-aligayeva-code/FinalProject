using HotelTravel.API.Services;
using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

/// <summary>Hotel partner (hotel staff account): which hotel do I manage?</summary>
[Route("api/partner")]
[Authorize(Roles = PartnerScope.PartnerRole)]
public class PartnerController : ApiControllerBase
{
    private readonly PartnerScope _scope;
    private readonly IHotelService _hotels;

    public PartnerController(PartnerScope scope, IHotelService hotels)
    {
        _scope = scope;
        _hotels = hotels;
    }

    // GET: api/partner/me
    /// <summary>The hotel of the signed-in partner.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(PartnerMeDto), 200)]
    public async Task<IActionResult> Me()
    {
        var hotelId = await _scope.HotelIdOfAsync(User);

        if (hotelId is null)
            return NotFound(ApiResultDto.Fail("This account is not linked to a hotel yet."));

        var hotel = await _hotels.GetByIdAsync(hotelId.Value);

        if (hotel is null)
            return NotFound(ApiResultDto.Fail("Hotel not found."));

        return Ok(new PartnerMeDto { HotelId = hotel.Id, HotelName = hotel.Name, City = hotel.City });
    }
}
