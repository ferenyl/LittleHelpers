using System.Text.Json;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace LittleHelpers.ApiService.Services.Notifications;

public sealed class FirebaseNotificationSender : IFirebaseNotificationSender
{
    private readonly ILogger<FirebaseNotificationSender> _logger;
    private readonly bool _active;

    public FirebaseNotificationSender(
        IOptions<FirebaseNotificationOptions> options,
        ILogger<FirebaseNotificationSender> logger)
    {
        _logger = logger;
        var config = options.Value;

        if (!config.Active)
        {
            _active = false;
            return;
        }

        if (!HasRequiredKeys(config))
        {
            _active = false;
            _logger.LogInformation(
                "Firebase notifications are disabled because one or more required keys are missing.");
            return;
        }

        _active = TryEnsureFirebaseApp(config);
    }

    public async Task SendToTopicAsync(string topic, string body, CancellationToken cancellationToken = default)
    {
        if (!_active)
            return;

        var message = new Message
        {
            Topic = topic,
            Notification = new Notification
            {
                Body = body
            }
        };

        await FirebaseMessaging.DefaultInstance.SendAsync(message, cancellationToken);
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

    private static bool HasRequiredKeys(FirebaseNotificationOptions config) =>
        !string.IsNullOrWhiteSpace(config.ProjectId)
        && !string.IsNullOrWhiteSpace(config.PrivateKey)
        && !string.IsNullOrWhiteSpace(config.ClientEmail);

    private static string NormalizePrivateKey(string privateKey) =>
        privateKey.Replace("\\n", "\n", StringComparison.Ordinal);

    private static string BuildClientCertUrl(string clientEmail) =>
        $"https://www.googleapis.com/robot/v1/metadata/x509/{Uri.EscapeDataString(clientEmail)}";
}
