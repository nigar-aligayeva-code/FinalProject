using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.HotelChains;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

[Route("api/hotel-chains")]
public class HotelChainsController : ApiControllerBase
{
    private readonly IHotelChainService _hotelChainService;

    public HotelChainsController(IHotelChainService hotelChainService)
    {
        _hotelChainService = hotelChainService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GetHotelChainDto>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _hotelChainService.GetAllAsync());

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetHotelChainDto), 200)]
    public async Task<IActionResult> GetById(int id)
        => OkOrNotFound(await _hotelChainService.GetByIdAsync(id), "Hotel chain not found.");

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateHotelChainDto dto)
    {
        await _hotelChainService.CreateAsync(dto);
        return Ok(ApiResultDto.Ok("Hotel chain created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHotelChainDto dto)
        => FromBool(await _hotelChainService.UpdateAsync(id, dto), "Hotel chain updated.", "Hotel chain not found.");

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _hotelChainService.DeleteAsync(id);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }
}
