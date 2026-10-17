using HotelTravel.API.Extensions;
using HotelTravel.Application.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

// Bütün API controller-lərinin ortaq bazası
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    // Daxil olmuş istifadəçinin Id-si (token yoxdursa null)
    protected string? CurrentUserId => User.GetUserId();

    // Uğurlu -> 200, uğursuz -> 400
    protected IActionResult FromResult(ApiResultDto result)
        => result.Success ? Ok(result) : BadRequest(result);

    // bool nəticə: tapıldı -> 200, tapılmadı -> 404
    protected IActionResult FromBool(
        bool ok,
        string successMessage,
        string failMessage = "Not found.")
        => ok
            ? Ok(ApiResultDto.Ok(successMessage))
            : NotFound(ApiResultDto.Fail(failMessage));

    protected IActionResult OkOrNotFound<T>(T? value, string message = "Not found.")
        where T : class
        => value is null
            ? NotFound(ApiResultDto.Fail(message))
            : Ok(value);
}
