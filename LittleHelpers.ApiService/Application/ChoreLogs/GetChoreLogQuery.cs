using LittleHelpers.ApiService.Application.Cqrs;

namespace LittleHelpers.ApiService.Application.ChoreLogs;

[RequireRoles("Parent", "Child")]
public sealed record GetChoreLogQuery(int ChildId, int? Year = null, int? Month = null) : IOwnedByCurrentUserRequest
{
    public int OwnerUserId => ChildId;
}

public sealed class GetChoreLogQueryHandler(
    IChoreLogRepository choreLogRepository,
    IDateTimeProvider dateTimeProvider,
    IMonthlyCycleService monthlyCycleService) : IQueryHandler<GetChoreLogQuery, ChoreLogPeriodDto>
{
    public async Task<ChoreLogPeriodDto> Handle(GetChoreLogQuery request, CancellationToken cancellationToken = default)
    {
        var now = dateTimeProvider.UtcNow;
        var period = request.Year is null && request.Month is null
            ? monthlyCycleService.GetCurrentPeriod(now)
            : monthlyCycleService.GetPeriodForMonth(
                request.Year ?? now.Year,
                request.Month ?? now.Month);

        var logs = await choreLogRepository.GetForChildAsync(
            request.ChildId,
            period.StartInclusive,
            period.EndExclusive);

        return new ChoreLogPeriodDto(
            logs,
            period.StartInclusive,
            period.EndExclusive);
    }
}
