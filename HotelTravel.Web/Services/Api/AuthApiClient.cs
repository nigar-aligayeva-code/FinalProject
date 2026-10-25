using System.Net.Http.Json;
using HotelTravel.Application.DTOs.Account;
using HotelTravel.Application.DTOs.Auth;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Email;

namespace HotelTravel.Web.Services.Api;

public interface IAuthApiClient
{
    Task<(AuthResponseDto? Auth, string? Error)> LoginAsync(string email, string password);

    Task<(AuthResponseDto? Auth, string? Error)> RegisterAsync(RegisterDto dto);

    Task<ApiResultDto> ForgotPasswordAsync(string email);

    Task<ApiResultDto> ResetPasswordAsync(ResetPasswordDto dto);

    Task<ApiResultDto> ConfirmEmailAsync(ConfirmEmailDto dto);

    Task<EmailCheckResult?> CheckEmailAsync(string email);
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

    public Task<ApiResultDto> ForgotPasswordAsync(string email)
        => PostForResultAsync("api/auth/forgot-password", new ForgotPasswordDto { Email = email });

    public Task<ApiResultDto> ResetPasswordAsync(ResetPasswordDto dto)
        => PostForResultAsync("api/auth/reset-password", dto);

    public Task<ApiResultDto> ConfirmEmailAsync(ConfirmEmailDto dto)
        => PostForResultAsync("api/auth/confirm-email", dto);

    // Qeydiyyat formunda canlı yoxlama; API cavab vermirsə null (forma bloklanmır)
    public async Task<EmailCheckResult?> CheckEmailAsync(string email)
    {
        try
        {
            return await _http.GetFromJsonAsync<EmailCheckResult>(
                $"api/auth/check-email?email={Uri.EscapeDataString(email)}", ApiClient.JsonOptions);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    // Cavab ApiResultDto-dur (token yoxdur)
    private async Task<ApiResultDto> PostForResultAsync(string url, object body)
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
            if ((int)response.StatusCode >= 500)
                return ApiResultDto.Fail("The server could not process the request. Please try again.");

            try
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResultDto>(ApiClient.JsonOptions);

                if (result != null && !string.IsNullOrWhiteSpace(result.Message))
                    return result;
            }
            catch
            {
                // validation cavabı
            }

            return response.IsSuccessStatusCode
                ? ApiResultDto.Ok()
                : ApiResultDto.Fail("Please check the form and try again.");
        }
    }

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
