using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Application.Children;

[RequireRoles("Parent", "Child")]
public sealed record GetChildDetailQuery(int ChildId) : IOwnedByCurrentUserRequest
{
    public int OwnerUserId => ChildId;
}

public sealed class GetChildDetailQueryHandler(
    IChildRepository childRepository,
    IChoreRepository choreRepository,
    IChoreLogRepository choreLogRepository,
    IChoreAvailabilityService availabilityService,
    IHttpContextAccessor httpContext,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetChildDetailQuery, ChildSummaryDto>
{
    public async Task<ChildSummaryDto> Handle(GetChildDetailQuery request, CancellationToken cancellationToken = default)
    {
        var now = dateTimeProvider.UtcNow;
        var isParent = httpContext.HttpContext?.User.IsInRole("Parent") == true;
        var child = await childRepository.GetChildWithAssignmentsAsync(request.ChildId)
            ?? throw new RequestNotFoundException("Barnet kunde inte hittas.");

        var lw = new LinkWriter<ChildSummaryDto>(httpContext)
            .AddLink("self", "GET", c => $"/children/{c.Id}");

        var completeLinks = new LinkWriter<ChoreDto>(httpContext)
            .AddLink("complete", "POST", c => $"/chores/{c.Id}/complete");

        var visibleAssignments = child.ChoreAssignments
            .Where(ca => !ca.Chore.IsHidden)
            .ToList();

        var logsByChoreId = new Dictionary<int, IReadOnlyList<ChoreLog>>();
        foreach (var assignment in visibleAssignments)
        {
            var logs = await choreLogRepository.GetLogsForChildAndChoreAsync(child.Id, assignment.Chore.Id);
            logsByChoreId[assignment.Chore.Id] = logs;
        }

        var chores = visibleAssignments
            .Select(ca =>
            {
                var dto = DtoFactory.CreateChoreDto(ca.Chore);
                var isCompletable = availabilityService.IsCompletable(ca.Chore, logsByChoreId[ca.Chore.Id], now);
                IEnumerable<Link> links = isCompletable || isParent
                    ? completeLinks.Build(dto)
                    : [];
                return dto with
                {
                    Links = links,
                    IsLimited = !isCompletable
                };
            });

        List<ChoreDto> parentChores = [];
        if (isParent)
        {
            var parentIds = await childRepository.GetParentIdsAsync();
            var parentChoreEntities = await choreRepository.GetParentChoresWithAssignmentsAsync(parentIds);
            var parentLogsByChoreId = new Dictionary<int, IReadOnlyList<ChoreLog>>();
            foreach (var chore in parentChoreEntities)
            {
                var logs = await choreLogRepository.GetLogsForChildAndChoreAsync(child.Id, chore.Id);
                parentLogsByChoreId[chore.Id] = logs;
            }

            parentChores = parentChoreEntities.Select(c =>
            {
                var dto = DtoFactory.CreateChoreDto(c);
                var isCompletable = availabilityService.IsCompletable(c, parentLogsByChoreId[c.Id], now);
                IEnumerable<Link> links = isCompletable || isParent
                    ? completeLinks.Build(dto)
                    : [];
                return dto with
                {
                    Links = links,
                    IsLimited = !isCompletable
                };
            }).ToList();
        }

        var summary = DtoFactory.CreateChildSummaryDto(child, chores.Concat(parentChores));
        return summary with { Links = lw.Build(summary) };
    }
}
