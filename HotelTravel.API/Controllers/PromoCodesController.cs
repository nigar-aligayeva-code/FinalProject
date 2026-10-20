using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.PromoCodes;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

/// <summary>Promo codes for discounts. Admin only.</summary>
[Route("api/promo-codes")]
[Authorize(Roles = "Admin")]
public class PromoCodesController : ApiControllerBase
{
    private readonly IPromoCodeService _promoCodeService;

    public PromoCodesController(IPromoCodeService promoCodeService)
    {
        _promoCodeService = promoCodeService;
    }

    /// <summary>List promo codes (admin).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<GetPromoCodeDto>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _promoCodeService.GetAllAsync());

    /// <summary>Create a promo code (admin).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePromoCodeDto dto)
    {
        var result = await _promoCodeService.CreateAsync(dto);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    /// <summary>Activate or deactivate a promo code (admin).</summary>
    [HttpPost("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
        => FromBool(await _promoCodeService.ToggleActiveAsync(id), "Promo code updated.", "Promo code not found.");

    /// <summary>Delete a promo code (admin).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => FromBool(await _promoCodeService.DeleteAsync(id), "Promo code deleted.", "Promo code not found.");
}
