using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Application.Chores;

[RequireRoles("Parent")]
public sealed record CreateChoreCommand(
    string Name,
    int Points,
    List<int> AssignedUserIds,
    int? MaxTimesPerDay = null,
    int? MinDaysBetween = null,
    int? MaxTimesPerWeek = null);

public sealed class CreateChoreCommandHandler(
    IChoreRepository choreRepository,
    IHttpContextAccessor httpContext) : ICommandHandler<CreateChoreCommand, ChoreDto>
{
    public async Task<ChoreDto> Handle(CreateChoreCommand request, CancellationToken cancellationToken = default)
    {
        var chore = new Chore
        {
            Name = request.Name,
            Points = request.Points,
            MaxTimesPerDay = request.MaxTimesPerDay,
            MinDaysBetween = request.MinDaysBetween,
            MaxTimesPerWeek = request.MaxTimesPerWeek
        };

        await choreRepository.AddAsync(chore);
        await choreRepository.SaveChangesAsync();

        await choreRepository.ReplaceAssignmentsAsync(chore, request.AssignedUserIds);
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
