namespace HotelTravel.Application.DTOs.Payments;

// MVC -> API: ödəniş səhifəsini aç
public class StartPaymentDto
{
    public string ConfirmationCode { get; set; } = string.Empty;

    // Stripe ödənişdən sonra bu ünvanlara qaytarır (MVC səhifələri)
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

// MVC -> API: Stripe-dan qayıdandan sonra ödənişi təsdiqlə
public class ConfirmPaymentDto
{
    public string SessionId { get; set; } = string.Empty;
}

public class PaymentResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    // Stripe-ın ödəniş səhifəsi (start üçün)
    public string? CheckoutUrl { get; set; }

    public string? ConfirmationCode { get; set; }

    public static PaymentResultDto Fail(string message) => new() { Success = false, Message = message };
}

// Gateway (Stripe) üçün sorğu və cavablar
public record PaymentCheckoutRequest(
    string ConfirmationCode,
    string Title,
    string Description,
    long AmountMinor,
    string Currency,
    string? CustomerEmail,
    string SuccessUrl,
    string CancelUrl);

public record PaymentCheckoutSession(string SessionId, string Url);

public record PaymentSessionStatus(bool IsPaid, string? ConfirmationCode, long AmountMinor, string Currency);
