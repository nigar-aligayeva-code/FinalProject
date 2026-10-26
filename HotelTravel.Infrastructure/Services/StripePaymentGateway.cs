using HotelTravel.Application.DTOs.Payments;
using HotelTravel.Application.Interfaces;
using Stripe;
using Stripe.Checkout;
using AppStripeOptions = HotelTravel.Application.Options.StripeOptions;

namespace HotelTravel.Infrastructure.Services;

/// <summary>
/// Stripe Checkout (hosted page). Kart məlumatı Stripe-ın səhifəsində yazılır —
/// bizim server kart nömrəsini heç vaxt görmür (PCI tələbi Stripe-dadır).
/// Test kartı: 4242 4242 4242 4242, istənilən gələcək tarix, istənilən CVC.
/// </summary>
public class StripePaymentGateway : IPaymentGateway
{
    private readonly AppStripeOptions _options;

    public StripePaymentGateway(AppStripeOptions options)
    {
        _options = options;
    }

    // Təhlükəsizlik: yalnız test açarı (sk_test_) ilə işləyir, real pul çəkilə bilməz
    public bool IsEnabled => _options.IsEnabled && _options.IsTestKey;

    private RequestOptions Request => new() { ApiKey = _options.SecretKey };

    public async Task<PaymentCheckoutSession> CreateCheckoutAsync(
        PaymentCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = request.CustomerEmail,
            ClientReferenceId = request.ConfirmationCode,

            // {CHECKOUT_SESSION_ID} Stripe tərəfindən real id ilə əvəz olunur
            SuccessUrl = AppendQuery(request.SuccessUrl, "session_id={CHECKOUT_SESSION_ID}"),
            CancelUrl = request.CancelUrl,

            Metadata = new Dictionary<string, string> { ["confirmation_code"] = request.ConfirmationCode },

            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency,
                        UnitAmount = request.AmountMinor,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.Title,
                            Description = request.Description
                        }
                    }
                }
            }
        };

        var session = await new SessionService().CreateAsync(options, Request, cancellationToken);

        return new PaymentCheckoutSession(session.Id, session.Url);
    }

    public async Task<PaymentSessionStatus> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await new SessionService().GetAsync(sessionId, null, Request, cancellationToken);

        session.Metadata.TryGetValue("confirmation_code", out var code);

        return new PaymentSessionStatus(
            string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase),
            code ?? session.ClientReferenceId,
            session.AmountTotal ?? 0,
            session.Currency ?? string.Empty);
    }

    public async Task<PaymentRefundResult> RefundAsync(string sessionId, long amountMinor, CancellationToken cancellationToken = default)
    {
        try
        {
            // Checkout session -> PaymentIntent -> qismən / tam refund
            var session = await new SessionService().GetAsync(sessionId, null, Request, cancellationToken);

            if (string.IsNullOrWhiteSpace(session.PaymentIntentId))
                return new PaymentRefundResult(false, null, "The payment was not found.");

            var refund = await new RefundService().CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = session.PaymentIntentId,
                Amount = amountMinor,
                Reason = "requested_by_customer"
            }, Request, cancellationToken);

            return new PaymentRefundResult(true, refund.Id, null);
        }
        catch (StripeException ex)
        {
            return new PaymentRefundResult(false, null, ex.Message);
        }
    }

    // "...?code=X" + "session_id=..." -> düzgün ? və ya &
    private static string AppendQuery(string url, string query) =>
        url + (url.Contains('?') ? "&" : "?") + query;
}
