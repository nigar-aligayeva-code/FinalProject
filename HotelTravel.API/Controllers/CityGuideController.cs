using HotelTravel.Application.DTOs.CityGuide;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

/// <summary>City guide: sights, tours, food and shopping per city.</summary>
[Route("api/city-guide")]
public class CityGuideController : ApiControllerBase
{
    private readonly ICityGuideService _cityGuideService;

    public CityGuideController(ICityGuideService cityGuideService)
    {
        _cityGuideService = cityGuideService;
    }

    // GET: api/city-guide/cities
    /// <summary>Cities that have a city guide.</summary>
    [HttpGet("cities")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> Cities()
        => Ok(await _cityGuideService.GetCitiesAsync());

    // GET: api/city-guide/city/Baku
    /// <summary>Guide places of one city.</summary>
    [HttpGet("city/{city}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CityPlaceDto>), 200)]
    public async Task<IActionResult> ByCity(string city)
        => Ok(await _cityGuideService.GetByCityAsync(city));

    // GET: api/city-guide?city=Baku  (admin siyahısı)
    /// <summary>All guide places, optionally filtered by city (admin).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<CityPlaceDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] string? city)
        => Ok(await _cityGuideService.GetAllAsync(city));

    // GET: api/city-guide/5/edit
    /// <summary>A guide place for the edit form (admin).</summary>
    [HttpGet("{id:int}/edit")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SaveCityPlaceDto), 200)]
    public async Task<IActionResult> GetForEdit(int id)
        => OkOrNotFound(await _cityGuideService.GetForEditAsync(id), "Place not found.");

    // POST: api/city-guide  (Id varsa yeniləyir, yoxdursa yaradır)
    /// <summary>Create a guide place, or update it if Id is set (admin).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] SaveCityPlaceDto dto)
    {
        await _cityGuideService.SaveAsync(dto);
        return Ok(ApiResultDto.Ok("Place saved."));
    }

    // DELETE: api/city-guide/5
    /// <summary>Delete a guide place (admin).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
        => FromBool(await _cityGuideService.DeleteAsync(id), "Place deleted.", "Place not found.");
}
