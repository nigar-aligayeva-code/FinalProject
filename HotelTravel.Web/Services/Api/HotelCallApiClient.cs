namespace HotelTravel.Web.Services.Api;

// Admin: otelə əsl telefon zəngi (API -> Twilio)
public interface IHotelCallApiClient
{
    Task<(bool Success, string Message)> CallHotelAsync(int bookingId);
}

public class HotelCallApiClient : IHotelCallApiClient
{
    private readonly ApiClient _api;

    public HotelCallApiClient(ApiClient api)
    {
        _api = api;
    }

    public async Task<(bool Success, string Message)> CallHotelAsync(int bookingId)
    {
        var result = await _api.PostAsync($"api/bookings/{bookingId}/call-hotel");
        return (result.Success, result.Message);
    }
}
