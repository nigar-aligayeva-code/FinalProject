using System.Diagnostics;

namespace HotelTravel.API.Middleware;

// Hər sorğunun müddətini loglayır və "X-Response-Time" başlığına yazır.
// Yavaş sorğular (500 ms-dən çox) xəbərdarlıq kimi görünür.
public class RequestTimingMiddleware
{
    private const int SlowRequestMs = 500;

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Response-Time"] = $"{stopwatch.ElapsedMilliseconds} ms";
            return Task.CompletedTask;
        });

        await _next(context);

        stopwatch.Stop();

        // Swagger faylları və SignalR bağlantısı logu doldurmasın
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/swagger") || path.StartsWith("/hubs"))
            return;

        var elapsed = stopwatch.ElapsedMilliseconds;

        if (elapsed > SlowRequestMs)
        {
            _logger.LogWarning("Slow request {Method} {Path} -> {StatusCode} in {Elapsed} ms",
                context.Request.Method, path, context.Response.StatusCode, elapsed);
        }
        else
        {
            _logger.LogInformation("{Method} {Path} -> {StatusCode} in {Elapsed} ms",
                context.Request.Method, path, context.Response.StatusCode, elapsed);
        }
    }
}
