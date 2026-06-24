using System.Security.Claims;
using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Services.Notifications;
using LittleHelpers.ApiService.Services.Realtime;

namespace LittleHelpers.ApiService.Application.ChoreLogs;

[RequireRoles("Parent")]
public sealed record DeleteChoreLogCommand(int ChoreLogId);

public sealed class DeleteChoreLogCommandHandler(
    IChoreLogRepository choreLogRepository,
    INotificationService notificationService,
    IHttpContextAccessor httpContext,
    IChildRealtimeNotifier realtimeNotifier) : ICommandHandler<DeleteChoreLogCommand, Unit>
{
    public async Task<Unit> Handle(DeleteChoreLogCommand request, CancellationToken cancellationToken = default)
    {
        var actorName = httpContext.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "Unknown user";
        var log = await choreLogRepository.GetTrackedByIdAsync(request.ChoreLogId)
            ?? throw new RequestNotFoundException("Historikposten kunde inte hittas.");
        var childId = log.ChildId;

        choreLogRepository.Remove(log);
        await choreLogRepository.SaveChangesAsync();
        await notificationService.NotifyPointsRemovedAsync(
            actorName,
            log.ChildId,
            log.Points,
            log.ChoreName,
            cancellationToken);
        await realtimeNotifier.NotifyChildUpdatedAsync(childId, "history_deleted", cancellationToken);
        return Unit.Value;
    }
}
