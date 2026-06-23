using LittleHelpers.ApiService.Application.Cqrs;

namespace LittleHelpers.ApiService.Application.ChoreLogs;

[RequireRoles("Parent")]
public sealed record DeleteChoreLogCommand(int ChoreLogId);

public sealed class DeleteChoreLogCommandHandler(
    IChoreLogRepository choreLogRepository) : ICommandHandler<DeleteChoreLogCommand, Unit>
{
    public async Task<Unit> Handle(DeleteChoreLogCommand request, CancellationToken cancellationToken = default)
    {
        var log = await choreLogRepository.GetTrackedByIdAsync(request.ChoreLogId)
            ?? throw new RequestNotFoundException("Historikposten kunde inte hittas.");

        choreLogRepository.Remove(log);
        await choreLogRepository.SaveChangesAsync();
        return Unit.Value;
    }
}
