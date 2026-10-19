using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

// Göndərilən məktubların jurnalı (admin)
[Route("api/emails")]
[Authorize(Roles = "Admin")]
public class EmailsController : ApiControllerBase
{
    private readonly IEmailLogService _emailLogService;

    public EmailsController(IEmailLogService emailLogService)
    {
        _emailLogService = emailLogService;
    }

    // GET: api/emails?take=50
    [HttpGet]
    [ProducesResponseType(typeof(List<EmailLogDto>), 200)]
    public async Task<IActionResult> GetRecent([FromQuery] int take = 50)
        => Ok(await _emailLogService.GetRecentAsync(take));

    // GET: api/emails/5  (HTML mətni ilə)
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmailLogDto), 200)]
    [ProducesResponseType(typeof(Application.DTOs.Common.ApiResultDto), 404)]
    public async Task<IActionResult> GetById(int id)
        => OkOrNotFound(await _emailLogService.GetByIdAsync(id), "Email not found.");
}
