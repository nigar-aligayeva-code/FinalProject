using HotelTravel.Application.DTOs.Common;

namespace HotelTravel.Web.Services.Api;

public interface IVoucherApiClient
{
    Task<byte[]?> GetPdfAsync(string confirmationCode);

    Task<ApiResultDto> SendEmailAsync(string confirmationCode);
}

// PDF voucher və təsdiq məktubu -> API (api/bookings/by-code/...)
public class VoucherApiClient : IVoucherApiClient
{
    private readonly ApiClient _api;

    public VoucherApiClient(ApiClient api)
    {
        _api = api;
    }

    public Task<byte[]?> GetPdfAsync(string confirmationCode)
        => _api.GetBytesOrNullAsync($"api/bookings/by-code/{ApiUrl.Segment(confirmationCode)}/voucher.pdf");

    public Task<ApiResultDto> SendEmailAsync(string confirmationCode)
        => _api.PostAsync($"api/bookings/by-code/{ApiUrl.Segment(confirmationCode)}/send-email");
}
