using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Application.Chores;

[RequireRoles("Parent")]
public sealed record UpdateChoreCommand(
    int ChoreId,
    string? Name,
    int? Points,
    bool? IsHidden,
    List<int>? AssignedUserIds,
    int? MaxTimesPerDay = null,
    int? MinDaysBetween = null,
    int? MaxTimesPerWeek = null);

public sealed class UpdateChoreCommandHandler(
    IChoreRepository choreRepository,
    IHttpContextAccessor httpContext) : ICommandHandler<UpdateChoreCommand, ChoreDto>
{
    public async Task<ChoreDto> Handle(UpdateChoreCommand request, CancellationToken cancellationToken = default)
    {
        var chore = await choreRepository.GetTrackedChoreWithAssignmentsAsync(request.ChoreId)
            ?? throw new RequestNotFoundException("Sysslan kunde inte hittas.");

        if (request.Name is not null) chore.Name = request.Name;
        if (request.Points is not null) chore.Points = request.Points.Value;
        if (request.IsHidden is not null) chore.IsHidden = request.IsHidden.Value;
        if (request.MaxTimesPerDay is not null) chore.MaxTimesPerDay = request.MaxTimesPerDay;
        if (request.MinDaysBetween is not null) chore.MinDaysBetween = request.MinDaysBetween;
        if (request.MaxTimesPerWeek is not null) chore.MaxTimesPerWeek = request.MaxTimesPerWeek;

        if (request.AssignedUserIds is not null)
        {
            await choreRepository.ReplaceAssignmentsAsync(chore, request.AssignedUserIds);
        }

        await choreRepository.SaveChangesAsync();

        var lw = new LinkWriter<ChoreDto>(httpContext)
            .AddLink("self", "GET", c => $"/chores/{c.Id}")
            .AddLinkForRole("Parent", "edit", "PUT", c => $"/chores/{c.Id}")
            .AddLinkForRole("Parent", "delete", "DELETE", c => $"/chores/{c.Id}")
            .AddLink("complete", "POST", c => $"/chores/{c.Id}/complete");

        var dto = DtoFactory.CreateChoreDto(chore);
        return dto with { Links = lw.Build(dto) };
    }
}
