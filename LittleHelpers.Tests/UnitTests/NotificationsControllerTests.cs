using System.Security.Claims;
using LittleHelpers.ApiService.Controllers;
using LittleHelpers.ApiService.Models;
using LittleHelpers.ApiService.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace LittleHelpers.Tests.UnitTests;

public class NotificationsControllerTests
{
    [Fact]
    public void GetWebConfig_ReturnsSenderConfiguration()
    {
        var sender = Substitute.For<IFirebaseNotificationSender>();
        var config = new FirebaseWebPushConfigurationDto(
            true,
            "api-key",
            "auth-domain",
            "project-id",
            "storage-bucket",
            "sender-id",
            "app-id",
            "vapid-key");
        sender.GetWebPushConfiguration().Returns(config);

        var controller = new NotificationsController(sender);

        var result = controller.GetWebConfig();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(config, ok.Value);
    }

    [Fact]
    public async Task Subscribe_ParentUser_SubscribesTokenToParentsTopic()
    {
        var sender = Substitute.For<IFirebaseNotificationSender>();
        sender.IsActive.Returns(true);
        var controller = CreateController(sender, 17, "Parent");
        var request = new WebPushSubscriptionRequest("registration-token");
        var cancellationToken = TestContext.Current.CancellationToken;

        var result = await controller.Subscribe(request, cancellationToken);

        Assert.IsType<NoContentResult>(result);
        await sender.Received(1).SubscribeToTopicAsync(
            NotificationTopics.Parents,
            request.RegistrationToken,
            cancellationToken);
    }

    [Fact]
    public async Task Subscribe_WhenSenderInactive_ReturnsServiceUnavailable()
    {
        var sender = Substitute.For<IFirebaseNotificationSender>();
        sender.IsActive.Returns(false);
        var controller = CreateController(sender, 17, "Parent");
        var request = new WebPushSubscriptionRequest("registration-token");

        var result = await controller.Subscribe(request, TestContext.Current.CancellationToken);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
    }

    [Fact]
    public async Task Unsubscribe_ChildUser_UsesChildTopic()
    {
        var sender = Substitute.For<IFirebaseNotificationSender>();
        sender.IsActive.Returns(true);
        var controller = CreateController(sender, 42, "Child");
        var request = new WebPushSubscriptionRequest("registration-token");
        var cancellationToken = TestContext.Current.CancellationToken;

        var result = await controller.Unsubscribe(request, cancellationToken);

        Assert.IsType<NoContentResult>(result);
        await sender.Received(1).UnsubscribeFromTopicAsync(
            NotificationTopics.Child(42),
            request.RegistrationToken,
            cancellationToken);
    }

    private static NotificationsController CreateController(
        IFirebaseNotificationSender sender,
        int userId,
        string role)
    {
        var controller = new NotificationsController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Role, role)
                    ], "test"))
                }
            }
        };

        return controller;
    }
}
