using HotelTravel.Application.DTOs.Amenities;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

[Route("api/amenities")]
public class AmenitiesController : ApiControllerBase
{
    private readonly IAmenityService _amenityService;

    public AmenitiesController(IAmenityService amenityService)
    {
        _amenityService = amenityService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GetAmenityDto>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _amenityService.GetAllAsync());

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetAmenityDto), 200)]
    public async Task<IActionResult> GetById(int id)
        => OkOrNotFound(await _amenityService.GetByIdAsync(id), "Amenity not found.");

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAmenityDto dto)
    {
        await _amenityService.CreateAsync(dto);
        return Ok(ApiResultDto.Ok("Amenity created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAmenityDto dto)
        => FromBool(await _amenityService.UpdateAsync(id, dto), "Amenity updated.", "Amenity not found.");

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _amenityService.DeleteAsync(id);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }
}
