using System.Security.Claims;
using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Models;
using LittleHelpers.ApiService.Services.Notifications;
using LittleHelpers.ApiService.Services.Realtime;
using Microsoft.EntityFrameworkCore;

namespace LittleHelpers.ApiService.Application.Chores;

[RequireRoles("Parent", "Child")]
public sealed record CompleteChoreCommand(int ChoreId, int? TargetChildId = null);

public sealed class CompleteChoreCommandHandler(
    IChoreRepository choreRepository,
    IChoreLogRepository choreLogRepository,
    IChoreAvailabilityService availabilityService,
    INotificationService notificationService,
    IChildRealtimeNotifier realtimeNotifier,
    IHttpContextAccessor httpContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CompleteChoreCommand, ChoreLogDto>
{
    public async Task<ChoreLogDto> Handle(CompleteChoreCommand request, CancellationToken cancellationToken = default)
    {
        var user = httpContext.HttpContext?.User ?? throw new RequestAuthorizationException("Missing authenticated user.");
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = user.FindFirstValue(ClaimTypes.Role)!;

        var chore = await choreRepository.GetChoreWithAssignmentsAsync(request.ChoreId)
            ?? throw new RequestNotFoundException("Sysslan kunde inte hittas.");

        int childId;
        if (userRole == "Child")
        {
            if (!chore.ChoreAssignments.Any(a => a.UserId == userId))
                throw new RequestAuthorizationException("Du är inte tilldelad den här sysslan.");
            childId = userId;
        }
        else
        {
            childId = request.TargetChildId ?? chore.ChoreAssignments.FirstOrDefault()?.UserId
                ?? throw new RequestValidationException("Sysslan har ingen tilldelad mottagare.");
        }

        var now = dateTimeProvider.UtcNow;
        var logs = await choreLogRepository.GetLogsForChildAndChoreAsync(childId, chore.Id);
        if (userRole != "Parent")
        {
            var blockReason = availabilityService.GetBlockReason(chore, logs, now);
            if (blockReason is not null)
                throw new RequestValidationException(blockReason);
        }

        var log = new ChoreLog
        {
            ChoreId = chore.Id,
            ChoreName = chore.Name,
            ChildId = childId,
            PerformedBy = userId,
            Points = chore.Points,
            Timestamp = now
        };

        await choreLogRepository.AddAsync(log);
        await choreLogRepository.SaveChangesAsync();
        var actorName = user.FindFirstValue(ClaimTypes.Name) ?? "Unknown user";
        await notificationService.NotifyPointsGivenAsync(actorName, childId, chore.Points, chore.Name, cancellationToken);
        await realtimeNotifier.NotifyChildUpdatedAsync(childId, "chore_completed", cancellationToken);

        return DtoFactory.CreateChoreLogDto(log, actorName);
    }
}
