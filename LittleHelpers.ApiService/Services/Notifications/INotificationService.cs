namespace LittleHelpers.ApiService.Services.Notifications;

public interface INotificationService
{
    Task NotifyPointsGivenAsync(
        string actorName,
        int childId,
        int chorePoints,
        string choreName,
        CancellationToken cancellationToken = default);

    Task NotifyPointsRemovedAsync(
        string actorName,
        int childId,
        int chorePoints,
        string choreName,
        CancellationToken cancellationToken = default);

    Task SendMorningNotificationsAsync(CancellationToken cancellationToken = default);
}
