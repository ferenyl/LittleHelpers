using LittleHelpers.ApiService.Application.Cqrs;

namespace LittleHelpers.ApiService.Application.Chores;

[RequireRoles("Parent")]
public sealed record DeleteChoreCommand(int ChoreId);

public sealed class DeleteChoreCommandHandler(
    IChoreRepository choreRepository) : ICommandHandler<DeleteChoreCommand, Unit>
{
    public async Task<Unit> Handle(DeleteChoreCommand request, CancellationToken cancellationToken = default)
    {
        var chore = await choreRepository.GetTrackedChoreWithAssignmentsAsync(request.ChoreId)
            ?? throw new RequestNotFoundException("Sysslan kunde inte hittas.");

        choreRepository.Remove(chore);
        await choreRepository.SaveChangesAsync();
        return Unit.Value;
    }
}
