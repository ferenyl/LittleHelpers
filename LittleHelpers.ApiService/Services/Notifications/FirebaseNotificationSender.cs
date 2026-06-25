using System.Text.Json;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace LittleHelpers.ApiService.Services.Notifications;

public sealed class FirebaseNotificationSender : IFirebaseNotificationSender
{
    private readonly ILogger<FirebaseNotificationSender> _logger;
    private readonly FirebaseNotificationOptions _config;
    private readonly bool _active;

    public FirebaseNotificationSender(
        IOptions<FirebaseNotificationOptions> options,
        ILogger<FirebaseNotificationSender> logger)
    {
        _logger = logger;
        _config = options.Value;

        if (!_config.Active)
        {
            _active = false;
            return;
        }

        if (!_config.HasRequiredAdminKeys())
        {
            _active = false;
            _logger.LogInformation(
                "Firebase notifications are disabled because one or more required keys are missing.");
            return;
        }

        _active = TryEnsureFirebaseApp(_config);
    }

    public bool IsActive => _active;

    public FirebaseWebPushConfigurationDto GetWebPushConfiguration() =>
        _config.ToWebPushConfiguration(_active);

    public async Task SendToTopicAsync(
        string topic,
        string title,
        string body,
        string? link = null,
        CancellationToken cancellationToken = default)
    {
        if (!_active)
            return;

        var absoluteLink = BuildAbsoluteLink(link);
        var message = new Message
        {
            Topic = topic,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = BuildData(link),
            Webpush = new WebpushConfig
            {
                Notification = new WebpushNotification
                {
                    Title = title,
                    Body = body,
                    Icon = "/icons/icon-192.png"
                },
                FcmOptions = absoluteLink is null
                    ? null
                    : new WebpushFcmOptions
                    {
                        Link = absoluteLink
                    }
            }
        };

        await FirebaseMessaging.DefaultInstance.SendAsync(message, cancellationToken);
    }

    public Task SubscribeToTopicAsync(
        string topic,
        string registrationToken,
        CancellationToken cancellationToken = default)
    {
        EnsureActive();
        cancellationToken.ThrowIfCancellationRequested();
        return FirebaseMessaging.DefaultInstance.SubscribeToTopicAsync(
            [registrationToken],
            topic);
    }

    public Task UnsubscribeFromTopicAsync(
        string topic,
        string registrationToken,
        CancellationToken cancellationToken = default)
    {
        EnsureActive();
        cancellationToken.ThrowIfCancellationRequested();
        return FirebaseMessaging.DefaultInstance.UnsubscribeFromTopicAsync(
            [registrationToken],
            topic);
    }

    private bool TryEnsureFirebaseApp(FirebaseNotificationOptions config)
    {
        try
        {
            if (FirebaseApp.DefaultInstance is not null)
                return true;
        }
        catch (InvalidOperationException)
        {
            // No default app exists yet.
        }

        try
        {
            var serviceAccount = new
            {
                type = "service_account",
                project_id = config.ProjectId,
                private_key_id = config.PrivateKeyId,
                private_key = NormalizePrivateKey(config.PrivateKey!),
                client_email = config.ClientEmail,
                client_id = config.ClientId,
                auth_uri = "https://accounts.google.com/o/oauth2/auth",
                token_uri = "https://oauth2.googleapis.com/token",
                auth_provider_x509_cert_url = "https://www.googleapis.com/oauth2/v1/certs",
                client_x509_cert_url = BuildClientCertUrl(config.ClientEmail!)
            };

            var json = JsonSerializer.Serialize(serviceAccount);
            var credential = GoogleCredential.FromJson(json);
            FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = config.ProjectId
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Firebase notifications are disabled due to invalid Firebase credentials.");
            return false;
        }
    }

    private static string NormalizePrivateKey(string privateKey) =>
        privateKey.Replace("\\n", "\n", StringComparison.Ordinal);

    private static string BuildClientCertUrl(string clientEmail) =>
        $"https://www.googleapis.com/robot/v1/metadata/x509/{Uri.EscapeDataString(clientEmail)}";

    private void EnsureActive()
    {
        if (!_active)
            throw new InvalidOperationException("Firebase notifications are not configured.");
    }

    private string? BuildAbsoluteLink(string? link)
    {
        if (string.IsNullOrWhiteSpace(link) || string.IsNullOrWhiteSpace(_config.WebAppUrl))
            return null;

        return new Uri(new Uri(_config.WebAppUrl.TrimEnd('/') + "/", UriKind.Absolute), link.TrimStart('/')).ToString();
    }

    private static Dictionary<string, string> BuildData(string? link)
    {
        var data = new Dictionary<string, string>
        {
            ["icon"] = "/icons/icon-192.png"
        };

        if (!string.IsNullOrWhiteSpace(link))
            data["link"] = link;

        return data;
    }
}
