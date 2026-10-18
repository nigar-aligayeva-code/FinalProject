using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelTravel.Application.DTOs.Common;

namespace HotelTravel.Web.Services.Api;

// MVC -> API əlaqəsi: bütün sorğular bu sinifdən (HttpClient) keçir.
// Token ApiTokenHandler tərəfindən avtomatik əlavə olunur.
public class ApiClient
{
    public static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    // =========================
    // GET
    // =========================

    // Məlumat mütləq olmalıdır (siyahılar, rəqəmlər)
    public async Task<T> GetAsync<T>(string url)
    {
        using var response = await SendRawAsync(HttpMethod.Get, url, null);

        await EnsureSuccessAsync(response);

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    // Tapılmasa null (404)
    public async Task<T?> GetOrNullAsync<T>(string url)
        where T : class
    {
        using var response = await SendRawAsync(HttpMethod.Get, url, null);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    // =========================
    // POST / PUT / DELETE -> ApiResultDto
    // =========================

    public Task<ApiResultDto> PostAsync(string url, object? body = null)
        => SendForResultAsync(HttpMethod.Post, url, body);

    public Task<ApiResultDto> PutAsync(string url, object? body = null)
        => SendForResultAsync(HttpMethod.Put, url, body);

    public Task<ApiResultDto> DeleteAsync(string url)
        => SendForResultAsync(HttpMethod.Delete, url, null);

    // POST, amma cavab məlumatdır (məs. axtarış nəticəsi)
    public async Task<T> PostForAsync<T>(string url, object body)
    {
        using var response = await SendRawAsync(HttpMethod.Post, url, body);

        await EnsureSuccessAsync(response);

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    // Uğursuz olarsa xəta at (void metodlar üçün: Create, Save)
    public static void EnsureOk(ApiResultDto result)
    {
        if (!result.Success)
            throw new ApiException(result.Message, 400);
    }

    // =========================
    // CORE
    // =========================

    private async Task<ApiResultDto> SendForResultAsync(HttpMethod method, string url, object? body)
    {
        using var response = await SendRawAsync(method, url, body);

        ThrowForAuth(response);

        if ((int)response.StatusCode >= 500)
            throw new ApiException(await ReadErrorAsync(response), (int)response.StatusCode);

        var result = await ReadResultAsync(response);

        // Cavabda ApiResultDto yoxdursa, statusa görə qərar ver
        return result ?? (response.IsSuccessStatusCode
            ? ApiResultDto.Ok()
            : ApiResultDto.Fail(await ReadErrorAsync(response)));
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string url, object? body)
    {
        var request = new HttpRequestMessage(method, url);

        if (body != null)
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);

        try
        {
            return await _http.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiUnavailableException(ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiUnavailableException(ex);
        }
    }

    private static void ThrowForAuth(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new ApiUnauthorizedException();

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new ApiForbiddenException();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        ThrowForAuth(response);

        if (!response.IsSuccessStatusCode)
            throw new ApiException(await ReadErrorAsync(response), (int)response.StatusCode);
    }

    // { success, message, ... } formatını oxu
    private static async Task<ApiResultDto?> ReadResultAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(text);

            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                HasProperty(doc.RootElement, "success"))
            {
                return JsonSerializer.Deserialize<ApiResultDto>(text, JsonOptions);
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    // Xəta mesajını oxu: ApiResultDto.message, ValidationProblem.errors və ya title
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var fallback = $"The API returned {(int)response.StatusCode} {response.ReasonPhrase}.";
        string text;

        try
        {
            text = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return fallback;
        }

        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return fallback;

            if (TryGetString(root, "message", out var message))
                return message;

            if (TryGetProperty(root, "errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                        ? p.Value.EnumerateArray().Select(v => v.GetString() ?? string.Empty)
                        : Array.Empty<string>())
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .ToList();

                if (messages.Count > 0)
                    return string.Join(" ", messages);
            }

            if (TryGetString(root, "detail", out var detail))
                return detail;

            if (TryGetString(root, "title", out var title))
                return title;
        }
        catch (JsonException)
        {
        }

        return fallback;
    }

    private static bool HasProperty(JsonElement element, string name)
        => TryGetProperty(element, name, out _);

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetString(JsonElement element, string name, out string value)
    {
        value = string.Empty;

        if (TryGetProperty(element, name, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
        {
            value = property.GetString()!;
            return true;
        }

        return false;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
