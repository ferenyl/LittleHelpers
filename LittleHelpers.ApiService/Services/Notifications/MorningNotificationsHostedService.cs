namespace LittleHelpers.ApiService.Services.Notifications;

public sealed class MorningNotificationsHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<MorningNotificationsHostedService> logger) : BackgroundService
{
    private static readonly TimeOnly RunAtLocal = new(7, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nowLocal = DateTime.Now;
            var nextRun = GetNextRunLocal(nowLocal);
            var delay = nextRun - nowLocal;

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await service.SendMorningNotificationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Morning notification job failed.");
            }
        }
    }

    private static DateTime GetNextRunLocal(DateTime nowLocal)
    {
        var todayAtRunTime = new DateTime(
            nowLocal.Year,
            nowLocal.Month,
            nowLocal.Day,
            RunAtLocal.Hour,
            RunAtLocal.Minute,
            0,
            DateTimeKind.Local);

        return nowLocal < todayAtRunTime
            ? todayAtRunTime
            : todayAtRunTime.AddDays(1);
    }
}
