using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Application.Chores;

[RequireRoles("Parent")]
public sealed record GetChoresQuery;

public sealed class GetChoresQueryHandler(
    IChoreRepository choreRepository,
    IHttpContextAccessor httpContext) : IQueryHandler<GetChoresQuery, IReadOnlyList<ChoreDto>>
{
    public async Task<IReadOnlyList<ChoreDto>> Handle(GetChoresQuery request, CancellationToken cancellationToken = default)
    {
        var chores = await choreRepository.GetChoresWithAssignmentsAsync();
        var lw = new LinkWriter<ChoreDto>(httpContext)
            .AddLink("self", "GET", c => $"/chores/{c.Id}")
            .AddLinkForRole("Parent", "edit", "PUT", c => $"/chores/{c.Id}")
            .AddLinkForRole("Parent", "delete", "DELETE", c => $"/chores/{c.Id}")
            .AddLink("complete", "POST", c => $"/chores/{c.Id}/complete");

        return chores.Select(c =>
        {
            var dto = DtoFactory.CreateChoreDto(c);
            return dto with { Links = lw.Build(dto) };
        }).ToList();
    }
}
