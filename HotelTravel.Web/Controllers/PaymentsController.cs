using HotelTravel.Application.DTOs.Payments;
using HotelTravel.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

// Onlayn ödəniş axını: Pay -> Stripe səhifəsi -> Success / Cancel -> Confirmation
public class PaymentsController : Controller
{
    private readonly IPaymentApiClient _payments;

    public PaymentsController(IPaymentApiClient payments)
    {
        _payments = payments;
    }

    // POST: /Payments/Pay  (rezervasiya kodu ilə Stripe-a yönləndir)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Pay(string confirmationCode) => StartAsync(confirmationCode);

    // GET: /Payments/Start?confirmationCode=...  (rezervasiya formundan "indi ödə" seçiləndə)
    [HttpGet]
    public Task<IActionResult> Start(string confirmationCode) => StartAsync(confirmationCode);

    private async Task<IActionResult> StartAsync(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return RedirectToAction("Find", "Bookings");

        var result = await _payments.StartAsync(new StartPaymentDto
        {
            ConfirmationCode = confirmationCode,
            SuccessUrl = Url.Action(nameof(Success), "Payments", null, Request.Scheme)!,
            CancelUrl = Url.Action(nameof(Cancel), "Payments", new { confirmationCode }, Request.Scheme)!
        });

        if (!result.Success || string.IsNullOrWhiteSpace(result.CheckoutUrl))
        {
            TempData["PaymentError"] = result.Message;
            return RedirectToAction("Confirmation", "Bookings", new { confirmationCode });
        }

        // Stripe-ın öz təhlükəsiz səhifəsi (checkout.stripe.com)
        return Redirect(result.CheckoutUrl);
    }

    // GET: /Payments/Success?session_id=cs_test_...
    [HttpGet]
    public async Task<IActionResult> Success([FromQuery(Name = "session_id")] string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return RedirectToAction("Index", "Home");

        // URL-ə etibar etmirik: API Stripe-dan ödənişin həqiqətən olduğunu yoxlayır
        var result = await _payments.ConfirmAsync(sessionId);

        TempData[result.Success ? "PaymentSuccess" : "PaymentError"] = result.Message;

        return string.IsNullOrWhiteSpace(result.ConfirmationCode)
            ? RedirectToAction("Find", "Bookings")
            : RedirectToAction("Confirmation", "Bookings", new { confirmationCode = result.ConfirmationCode });
    }

    // GET: /Payments/Cancel?confirmationCode=...
    [HttpGet]
    public IActionResult Cancel(string confirmationCode)
    {
        TempData["PaymentError"] = "Payment was cancelled. Your booking is saved — you can pay later or at the hotel.";

        return RedirectToAction("Confirmation", "Bookings", new { confirmationCode });
    }
}
