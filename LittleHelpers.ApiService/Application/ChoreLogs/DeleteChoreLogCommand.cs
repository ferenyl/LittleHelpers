using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Services.Realtime;

namespace LittleHelpers.ApiService.Application.ChoreLogs;

[RequireRoles("Parent")]
public sealed record DeleteChoreLogCommand(int ChoreLogId);

public sealed class DeleteChoreLogCommandHandler(
    IChoreLogRepository choreLogRepository,
    IChildRealtimeNotifier realtimeNotifier) : ICommandHandler<DeleteChoreLogCommand, Unit>
{
    public async Task<Unit> Handle(DeleteChoreLogCommand request, CancellationToken cancellationToken = default)
    {
        var log = await choreLogRepository.GetTrackedByIdAsync(request.ChoreLogId)
            ?? throw new RequestNotFoundException("Historikposten kunde inte hittas.");
        var childId = log.ChildId;

        choreLogRepository.Remove(log);
        await choreLogRepository.SaveChangesAsync();
        await realtimeNotifier.NotifyChildUpdatedAsync(childId, "history_deleted", cancellationToken);
        return Unit.Value;
    }
}
