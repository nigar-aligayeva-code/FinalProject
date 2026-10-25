using HotelTravel.Application.DTOs.Payments;
using Microsoft.Extensions.Caching.Memory;

namespace HotelTravel.Web.Services.Api;

public interface IPaymentApiClient
{
    Task<bool> IsEnabledAsync();
    Task<PaymentResultDto> StartAsync(StartPaymentDto dto);
    Task<PaymentResultDto> ConfirmAsync(string sessionId);
}

// Ödəniş -> API (api/payments). Stripe açarı yalnız API-dədir, MVC onu görmür.
public class PaymentApiClient : IPaymentApiClient
{
    private readonly ApiClient _api;
    private readonly IMemoryCache _cache;

    public PaymentApiClient(ApiClient api, IMemoryCache cache)
    {
        _api = api;
        _cache = cache;
    }

    // Hər səhifədə API-ni yormamaq üçün 1 dəqiqə yadda saxlanılır
    public async Task<bool> IsEnabledAsync()
    {
        return await _cache.GetOrCreateAsync("payments-enabled", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            try
            {
                var status = await _api.GetAsync<PaymentStatusResponse>("api/payments/status");
                return status.Enabled;
            }
            catch (Exception)
            {
                return false;
            }
        });
    }

    public Task<PaymentResultDto> StartAsync(StartPaymentDto dto)
        => PostAsync("api/payments/checkout", dto);

    public Task<PaymentResultDto> ConfirmAsync(string sessionId)
        => PostAsync("api/payments/confirm", new ConfirmPaymentDto { SessionId = sessionId });

    private async Task<PaymentResultDto> PostAsync(string url, object body)
    {
        try
        {
            return await _api.PostForAsync<PaymentResultDto>(url, body);
        }
        catch (ApiException ex)
        {
            return PaymentResultDto.Fail(ex.Message);
        }
    }

    private sealed class PaymentStatusResponse
    {
        public bool Enabled { get; set; }
    }
}
