namespace HotelTravel.Web.Services.Api;

public interface IExportApiClient
{
    Task<byte[]?> DownloadAsync(string type, string format);
}

// Admin hesabatları -> API (api/exports/{type}?format=xlsx|csv)
public class ExportApiClient : IExportApiClient
{
    private readonly ApiClient _api;

    public ExportApiClient(ApiClient api)
    {
        _api = api;
    }

    public Task<byte[]?> DownloadAsync(string type, string format)
        => _api.GetBytesOrNullAsync(ApiUrl.For($"api/exports/{ApiUrl.Segment(type)}").Add("format", format));
}
