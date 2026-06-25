namespace LittleHelpers.ApiService.Services.Notifications;

public static class NotificationTopics
{
    public const string Parents = "parents";

    public static string Child(int childId) => $"child-{childId}";
}
