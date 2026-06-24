using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Application.Children;

[RequireRoles("Parent")]
public sealed record GetChildrenQuery;

public sealed class GetChildrenQueryHandler(
    IChildRepository childRepository,
    IChoreLogRepository choreLogRepository,
    IHttpContextAccessor httpContext,
    IDateTimeProvider dateTimeProvider,
    IMonthlyCycleService monthlyCycleService) : IQueryHandler<GetChildrenQuery, IReadOnlyList<ChildSummaryDto>>
{
    public async Task<IReadOnlyList<ChildSummaryDto>> Handle(GetChildrenQuery request, CancellationToken cancellationToken = default)
    {
        var children = await childRepository.GetChildrenWithAssignmentsAsync();
        var period = monthlyCycleService.GetCurrentPeriod(dateTimeProvider.UtcNow);
        var childIds = children.Select(c => c.Id).ToList();
        var monthPoints = await choreLogRepository.GetMonthlyPointsAsync(
            childIds,
            period.StartInclusive,
            period.EndExclusive);

        var lw = new LinkWriter<ChildSummaryDto>(httpContext)
            .AddLink("self", "GET", c => $"/children/{c.Id}");

        var choreLw = new LinkWriter<ChoreDto>(httpContext)
            .AddLink("complete", "POST", c => $"/chores/{c.Id}/complete");

        return children.Select(c =>
        {
            var chores = c.ChoreAssignments
                .Where(ca => !ca.Chore.IsHidden)
                .Select(ca =>
                {
                    var dto = DtoFactory.CreateChoreDto(ca.Chore);
                    return dto with { Links = choreLw.Build(dto) };
                });

            var summary = DtoFactory.CreateChildSummaryDto(c, chores, monthPoints.GetValueOrDefault(c.Id, 0));
            return summary with { Links = lw.Build(summary) };
        }).ToList();
    }
}
