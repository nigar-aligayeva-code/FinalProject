using System.Net.Http.Headers;
using HotelTravel.Web.Services;

namespace HotelTravel.Web.Services.Api;

// Daxil olmuş istifadəçinin JWT token-ini hər API sorğusuna əlavə edir
public class ApiTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.User
            .FindFirst(AuthClaimTypes.AccessToken)?.Value;

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}
