namespace LittleHelpers.ApiService.Services.Notifications;

public interface IFirebaseNotificationSender
{
    bool IsActive { get; }
    FirebaseWebPushConfigurationDto GetWebPushConfiguration();
    Task SendToTopicAsync(string topic, string title, string body, string? link = null, CancellationToken cancellationToken = default);
    Task SubscribeToTopicAsync(string topic, string registrationToken, CancellationToken cancellationToken = default);
    Task UnsubscribeFromTopicAsync(string topic, string registrationToken, CancellationToken cancellationToken = default);
}
