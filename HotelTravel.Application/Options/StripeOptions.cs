namespace HotelTravel.Application.Options;

// appsettings / User Secrets -> "Stripe" bölməsi.
// SecretKey yoxdursa onlayn ödəniş söndürülüdür (qonaq otelə gələndə ödəyir).
// Test açarları: https://dashboard.stripe.com/test/apikeys  (sk_test_...)
public class StripeOptions
{
    public string? SecretKey { get; set; }

    // Stripe-ın qəbul etdiyi valyuta kodu (kiçik hərflə)
    public string Currency { get; set; } = "azn";

    public bool IsEnabled => !string.IsNullOrWhiteSpace(SecretKey);

    // Canlı (real pul) açarların təsadüfən istifadəsinin qarşısı: yalnız test açarı
    public bool IsTestKey => SecretKey?.StartsWith("sk_test_", StringComparison.Ordinal) == true;
}
