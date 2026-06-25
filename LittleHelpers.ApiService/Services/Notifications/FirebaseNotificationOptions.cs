namespace LittleHelpers.ApiService.Services.Notifications;

public sealed class FirebaseNotificationOptions
{
    public const string SectionName = "FirebaseNotifications";

    public bool Active { get; init; }
    public string? ProjectId { get; init; }
    public string? PrivateKeyId { get; init; }
    public string? PrivateKey { get; init; }
    public string? ClientEmail { get; init; }
    public string? ClientId { get; init; }
    public string? WebAppUrl { get; init; }
    public string? WebApiKey { get; init; }
    public string? WebAuthDomain { get; init; }
    public string? WebStorageBucket { get; init; }
    public string? WebMessagingSenderId { get; init; }
    public string? WebAppId { get; init; }
    public string? WebVapidKey { get; init; }

    public bool HasRequiredAdminKeys() =>
        !string.IsNullOrWhiteSpace(ProjectId)
        && !string.IsNullOrWhiteSpace(PrivateKey)
        && !string.IsNullOrWhiteSpace(ClientEmail);

    public bool HasRequiredWebKeys() =>
        !string.IsNullOrWhiteSpace(WebApiKey)
        && !string.IsNullOrWhiteSpace(WebAuthDomain)
        && !string.IsNullOrWhiteSpace(ProjectId)
        && !string.IsNullOrWhiteSpace(WebStorageBucket)
        && !string.IsNullOrWhiteSpace(WebMessagingSenderId)
        && !string.IsNullOrWhiteSpace(WebAppId)
        && !string.IsNullOrWhiteSpace(WebVapidKey);

    public FirebaseWebPushConfigurationDto ToWebPushConfiguration(bool enabled) =>
        new(
            enabled && HasRequiredWebKeys(),
            WebApiKey ?? string.Empty,
            WebAuthDomain ?? string.Empty,
            ProjectId ?? string.Empty,
            WebStorageBucket ?? string.Empty,
            WebMessagingSenderId ?? string.Empty,
            WebAppId ?? string.Empty,
            WebVapidKey ?? string.Empty);
}
