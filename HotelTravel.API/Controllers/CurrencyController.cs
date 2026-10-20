using HotelTravel.Application.DTOs.Currency;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

/// <summary>Exchange rates for showing prices in other currencies. Payments are always in AZN.</summary>
[Route("api/currency")]
[AllowAnonymous]
public class CurrencyController : ApiControllerBase
{
    private readonly ICurrencyService _currencyService;

    public CurrencyController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    // GET: api/currency/rates
    /// <summary>How much 1 AZN is in USD, EUR, TRY, GEL and AED (live rates, cached for 6 hours; approximate rates if offline).</summary>
    [HttpGet("rates")]
    [ResponseCache(Duration = 600)]
    [ProducesResponseType(typeof(CurrencyRatesDto), 200)]
    public async Task<IActionResult> Rates()
        => Ok(await _currencyService.GetRatesAsync());
}
