using HotelTravel.Application.DTOs.Cars;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

[Route("api/cars")]
public class CarsController : ApiControllerBase
{
    private readonly ICarRentalService _carRentalService;
    private readonly IActivityNotifier _notifier;

    public CarsController(ICarRentalService carRentalService, IActivityNotifier notifier)
    {
        _carRentalService = carRentalService;
        _notifier = notifier;
    }

    // =========================
    // PUBLIC
    // =========================

    // GET: api/cars/cities
    [HttpGet("cities")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> Cities()
        => Ok(await _carRentalService.GetCitiesAsync());

    // GET: api/cars/search?city=Baku&pickupDate=...&returnDate=...
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CarDto>), 200)]
    public async Task<IActionResult> Search(
        [FromQuery] string city,
        [FromQuery] DateTime? pickupDate,
        [FromQuery] DateTime? returnDate)
        => Ok(await _carRentalService.SearchAsync(city, pickupDate, returnDate));

    // GET: api/cars/5?pickupDate=...&returnDate=...
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CarDto), 200)]
    public async Task<IActionResult> GetCar(
        int id,
        [FromQuery] DateTime? pickupDate,
        [FromQuery] DateTime? returnDate)
        => OkOrNotFound(await _carRentalService.GetCarAsync(id, pickupDate, returnDate), "Car not found.");

    // POST: api/cars/rentals
    [HttpPost("rentals")]
    [AllowAnonymous]
    public async Task<IActionResult> Book([FromBody] CreateCarRentalDto dto)
    {
        var result = await _carRentalService.BookAsync(dto, CurrentUserId);

        if (result.Success && result.ConfirmationCode != null)
            await _notifier.CarRentedAsync(result.ConfirmationCode);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message, code: result.ConfirmationCode)
            : ApiResultDto.Fail(result.Message));
    }

    // GET: api/cars/rentals/by-code/CR-1234ABCD
    [HttpGet("rentals/by-code/{confirmationCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CarRentalDto), 200)]
    public async Task<IActionResult> GetByCode(string confirmationCode)
        => OkOrNotFound(await _carRentalService.GetByCodeAsync(confirmationCode), "Car rental not found.");

    // GET: api/cars/rentals/mine
    [HttpGet("rentals/mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<CarRentalDto>), 200)]
    public async Task<IActionResult> Mine()
        => Ok(await _carRentalService.GetByUserAsync(CurrentUserId!));

    // POST: api/cars/rentals/5/cancel-mine
    [HttpPost("rentals/{id:int}/cancel-mine")]
    [Authorize]
    public async Task<IActionResult> CancelMine(int id)
    {
        var result = await _carRentalService.CancelByUserAsync(id, CurrentUserId!);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // =========================
    // ADMIN
    // =========================

    // GET: api/cars/fleet?city=Baku
    [HttpGet("fleet")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<CarDto>), 200)]
    public async Task<IActionResult> Fleet([FromQuery] string? city)
        => Ok(await _carRentalService.GetFleetAsync(city));

    // GET: api/cars/5/edit
    [HttpGet("{id:int}/edit")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SaveCarDto), 200)]
    public async Task<IActionResult> GetForEdit(int id)
        => OkOrNotFound(await _carRentalService.GetCarForEditAsync(id), "Car not found.");

    // POST: api/cars/fleet  (Id varsa yeniləyir)
    [HttpPost("fleet")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveCar([FromBody] SaveCarDto dto)
    {
        await _carRentalService.SaveCarAsync(dto);
        return Ok(ApiResultDto.Ok("Car saved."));
    }

    // POST: api/cars/5/toggle
    [HttpPost("{id:int}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Toggle(int id)
        => FromBool(await _carRentalService.ToggleCarAsync(id), "Car updated.", "Car not found.");

    // GET: api/cars/rentals
    [HttpGet("rentals")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<CarRentalDto>), 200)]
    public async Task<IActionResult> Rentals()
        => Ok(await _carRentalService.GetRentalsAsync());

    // PUT: api/cars/rentals/5/status
    [HttpPut("rentals/{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RentalStatus(
        int id,
        [FromBody] StatusUpdateDto<CarRentalStatus> dto)
        => FromBool(
            await _carRentalService.SetRentalStatusAsync(id, dto.Status),
            "Rental updated.",
            "This rental cannot be changed.");
}
