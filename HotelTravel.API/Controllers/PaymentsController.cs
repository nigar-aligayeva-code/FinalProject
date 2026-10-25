using HotelTravel.Application.DTOs.Payments;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotelTravel.API.Controllers;

/// <summary>Online payment for bookings (Stripe Checkout, test mode).</summary>
[Route("api/payments")]
[AllowAnonymous]
public class PaymentsController : ApiControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments)
    {
        _payments = payments;
    }

    // GET: api/payments/status
    /// <summary>Is online payment enabled? (a Stripe test key is configured)</summary>
    [HttpGet("status")]
    [ProducesResponseType(200)]
    public IActionResult Status() => Ok(new { enabled = _payments.IsEnabled });

    // POST: api/payments/checkout
    /// <summary>Create a Stripe Checkout page for a booking and return its URL.</summary>
    [HttpPost("checkout")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(PaymentResultDto), 200)]
    [ProducesResponseType(typeof(PaymentResultDto), 400)]
    public async Task<IActionResult> Checkout([FromBody] StartPaymentDto dto)
    {
        var result = await _payments.StartAsync(dto, HttpContext.RequestAborted);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST: api/payments/confirm
    /// <summary>Verify the payment with Stripe after the guest returns, then mark the booking as paid.</summary>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(PaymentResultDto), 200)]
    [ProducesResponseType(typeof(PaymentResultDto), 400)]
    public async Task<IActionResult> Confirm([FromBody] ConfirmPaymentDto dto)
    {
        var result = await _payments.ConfirmAsync(dto, HttpContext.RequestAborted);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
