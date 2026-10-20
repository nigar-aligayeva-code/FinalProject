using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Weather;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

// Səfər tarixləri üçün hava (Open-Meteo üzərindən)
[Route("api/weather")]
[AllowAnonymous]
public class WeatherController : ApiControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    // GET: api/weather/hotel/5?from=2026-11-10&to=2026-11-14
    [HttpGet("hotel/{hotelId:int}")]
    [ProducesResponseType(typeof(TripWeatherDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 404)]
    public async Task<IActionResult> ForHotel(int hotelId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => OkOrNotFound(await _weatherService.GetForHotelAsync(hotelId, from, to), "Weather is not available.");

    // GET: api/weather/city/Istanbul?from=...&to=...
    [HttpGet("city/{city}")]
    [ProducesResponseType(typeof(TripWeatherDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 404)]
    public async Task<IActionResult> ForCity(string city, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => OkOrNotFound(await _weatherService.GetForCityAsync(city, from, to), "Weather is not available.");
}
