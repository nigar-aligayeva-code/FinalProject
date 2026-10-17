using System.Net.Http.Json;
using HotelTravel.Application.DTOs.Account;
using HotelTravel.Application.DTOs.Auth;
using HotelTravel.Application.DTOs.Common;

namespace HotelTravel.Web.Services.Api;

public interface IAuthApiClient
{
    Task<(AuthResponseDto? Auth, string? Error)> LoginAsync(string email, string password);

    Task<(AuthResponseDto? Auth, string? Error)> RegisterAsync(RegisterDto dto);
}

// Login / register: API-dən JWT token alır (api/auth)
public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<(AuthResponseDto? Auth, string? Error)> LoginAsync(string email, string password)
        => PostAsync("api/auth/login", new { email, password }, "Email or password is incorrect.");

    public Task<(AuthResponseDto? Auth, string? Error)> RegisterAsync(RegisterDto dto)
        => PostAsync("api/auth/register", dto, "Registration failed.");

    private async Task<(AuthResponseDto? Auth, string? Error)> PostAsync(
        string url,
        object body,
        string defaultError)
    {
        HttpResponseMessage response;

        try
        {
            response = await _http.PostAsJsonAsync(url, body, ApiClient.JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiUnavailableException(ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiUnavailableException(ex);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>(ApiClient.JsonOptions);

                return auth != null && !string.IsNullOrWhiteSpace(auth.AccessToken)
                    ? (auth, null)
                    : (null, defaultError);
            }

            if ((int)response.StatusCode >= 500)
                return (null, "The server could not process the request. Please try again.");

            try
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResultDto>(ApiClient.JsonOptions);

                if (!string.IsNullOrWhiteSpace(result?.Message))
                    return (null, result!.Message);
            }
            catch
            {
                // Validation cavabı (errors) gəlibsə, ümumi mesaj
            }

            return (null, defaultError);
        }
    }
}
