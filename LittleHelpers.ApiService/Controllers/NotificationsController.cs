using System.Security.Claims;
using LittleHelpers.ApiService.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("notifications")]
public sealed class NotificationsController(
    IFirebaseNotificationSender firebaseSender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("web-config")]
    public ActionResult<FirebaseWebPushConfigurationDto> GetWebConfig() =>
        Ok(firebaseSender.GetWebPushConfiguration());

    [HttpPost("subscriptions")]
    public async Task<IActionResult> Subscribe(
        [FromBody] WebPushSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!firebaseSender.IsActive)
            return Problem("Firebase notifications are not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);

        await firebaseSender.SubscribeToTopicAsync(
            GetCurrentUserTopic(),
            request.RegistrationToken,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("subscriptions/remove")]
    public async Task<IActionResult> Unsubscribe(
        [FromBody] WebPushSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!firebaseSender.IsActive)
            return Problem("Firebase notifications are not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);

        await firebaseSender.UnsubscribeFromTopicAsync(
            GetCurrentUserTopic(),
            request.RegistrationToken,
            cancellationToken);

        return NoContent();
    }

    private string GetCurrentUserTopic()
    {
        var user = HttpContext.User;
        var role = user.FindFirstValue(ClaimTypes.Role)
            ?? throw new RequestAuthorizationException("Missing user role claim.");

        return role switch
        {
            "Parent" => NotificationTopics.Parents,
            "Child" => NotificationTopics.Child(GetCurrentUserId(user)),
            _ => throw new RequestAuthorizationException("User role is not allowed to receive notifications.")
        };
    }

    private static int GetCurrentUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            throw new RequestAuthorizationException("Missing user identifier claim.");

        return userId;
    }
}
