using LittleHelpers.ApiService.Application.Cqrs;

namespace LittleHelpers.ApiService.Application.ChoreLogs;

[RequireRoles("Parent", "Child")]
public sealed record GetChoreLogQuery(int ChildId, int? Year = null, int? Month = null) : IOwnedByCurrentUserRequest
{
    public int OwnerUserId => ChildId;
}

public sealed class GetChoreLogQueryHandler(
    IChoreLogRepository choreLogRepository,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetChoreLogQuery, IReadOnlyList<ChoreLogDto>>
{
    public async Task<IReadOnlyList<ChoreLogDto>> Handle(GetChoreLogQuery request, CancellationToken cancellationToken = default)
    {
        var now = dateTimeProvider.UtcNow;
        var filterYear = request.Year ?? now.Year;
        var filterMonth = request.Month ?? now.Month;
        return await choreLogRepository.GetForChildAsync(request.ChildId, filterYear, filterMonth);
    }
}
