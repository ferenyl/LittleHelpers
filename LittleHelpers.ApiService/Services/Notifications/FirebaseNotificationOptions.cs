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
}
