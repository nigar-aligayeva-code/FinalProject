using HotelTravel.Application.DTOs.Payments;

namespace HotelTravel.Application.Interfaces;

/// <summary>Ödəniş provayderi (Stripe). Application Stripe-dan asılı deyil — yalnız bu interfeys.</summary>
public interface IPaymentGateway
{
    bool IsEnabled { get; }

    Task<PaymentCheckoutSession> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken = default);

    Task<PaymentSessionStatus> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    // Ləğvdən sonra pulu (cərimə çıxılmaqla) karta qaytar
    Task<PaymentRefundResult> RefundAsync(string sessionId, long amountMinor, CancellationToken cancellationToken = default);
}

public interface IPaymentService
{
    bool IsEnabled { get; }

    Task<PaymentResultDto> StartAsync(StartPaymentDto dto, CancellationToken cancellationToken = default);

    Task<PaymentResultDto> ConfirmAsync(ConfirmPaymentDto dto, CancellationToken cancellationToken = default);

    // Ləğv edilmiş, onlayn ödənilmiş rezervasiyanın pulunu (cərimə çıxılmaqla) qaytar
    Task<PaymentResultDto> RefundCancelledAsync(string confirmationCode, CancellationToken cancellationToken = default);
}
