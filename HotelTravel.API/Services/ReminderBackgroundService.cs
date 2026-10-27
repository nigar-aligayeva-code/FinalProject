using HotelTravel.Application.Interfaces;

namespace HotelTravel.API.Services;

/// <summary>
/// Fon xidməti: hər N dəqiqədən bir vaxtı çatmış xatırlatmaları göndərir
/// (girişə 3 gün / 1 gün, uçuşa 24 saat / 3 saat qalmış)
/// və otelin 24 saatda cavab vermədiyi sorğuları bağlayır.
/// appsettings: "Reminders": { "Enabled": true, "IntervalMinutes": 10 }
/// </summary>
public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(
        IServiceScopeFactory scopes,
        IConfiguration config,
        ILogger<ReminderBackgroundService> logger)
    {
        _scopes = scopes;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue("Reminders:Enabled", true))
            return;

        var interval = TimeSpan.FromMinutes(Math.Max(1, _config.GetValue("Reminders:IntervalMinutes", 10)));

        // API tam açılsın (migration, seed) - sonra başlayaq
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var reminders = scope.ServiceProvider.GetRequiredService<IReminderService>();

                var sent = await reminders.SendDueAsync(DateTime.Now, stoppingToken);

                if (sent > 0)
                    _logger.LogInformation("Reminders sent: {Count}", sent);

                // Otel 24 saatda cavab verməyən sorğular avtomatik bağlanır
                var expiry = scope.ServiceProvider.GetRequiredService<IRequestExpiryService>();
                var expired = await expiry.ExpireOverdueAsync(DateTime.UtcNow, stoppingToken);

                if (expired > 0)
                    _logger.LogInformation("Expired booking requests: {Count}", expired);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Xatırlatma alınmasa API işləməyə davam edir
                _logger.LogWarning(ex, "Reminders could not be sent.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
