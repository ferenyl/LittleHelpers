namespace LittleHelpers.ApiService.Services.Notifications;

public interface IFirebaseNotificationSender
{
    Task SendToTopicAsync(string topic, string body, CancellationToken cancellationToken = default);
}
