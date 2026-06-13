using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Application.Chores;

[RequireRoles("Parent")]
public sealed record GetChoreByIdQuery(int ChoreId);

public sealed class GetChoreByIdQueryHandler(
    IChoreRepository choreRepository,
    IHttpContextAccessor httpContext) : IQueryHandler<GetChoreByIdQuery, ChoreDto>
{
    public async Task<ChoreDto> Handle(GetChoreByIdQuery request, CancellationToken cancellationToken = default)
    {
        var chore = await choreRepository.GetChoreWithAssignmentsAsync(request.ChoreId)
            ?? throw new RequestNotFoundException("Sysslan kunde inte hittas.");

        var lw = new LinkWriter<ChoreDto>(httpContext)
            .AddLink("self", "GET", c => $"/chores/{c.Id}")
            .AddLinkForRole("Parent", "edit", "PUT", c => $"/chores/{c.Id}")
            .AddLinkForRole("Parent", "delete", "DELETE", c => $"/chores/{c.Id}")
            .AddLink("complete", "POST", c => $"/chores/{c.Id}/complete");

        var dto = DtoFactory.CreateChoreDto(chore);
        return dto with { Links = lw.Build(dto) };
    }
}
