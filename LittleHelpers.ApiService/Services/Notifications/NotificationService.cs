using Microsoft.Extensions.Options;

namespace LittleHelpers.ApiService.Services.Notifications;

public sealed class NotificationService(
    IFirebaseNotificationSender firebaseSender,
    IUserRepository userRepository,
    IChildRepository childRepository,
    IChoreLogRepository choreLogRepository,
    IDateTimeProvider dateTimeProvider,
    IMonthlyCycleService monthlyCycleService,
    IOptions<MonthlyCycleOptions> monthlyCycleOptions,
    ILogger<NotificationService> logger) : INotificationService
{
    private const string ParentsTopic = "parents";

    public async Task NotifyPointsGivenAsync(
        string actorName,
        int childId,
        int chorePoints,
        string choreName,
        CancellationToken cancellationToken = default)
    {
        var child = await userRepository.GetByIdAsync(childId);
        if (child is null)
            return;

        var forParents = $"{actorName} has given {child.Username} {chorePoints} {choreName}";
        var forChild = $"{child.Username} has been given {chorePoints} by doing {choreName}";
        await TrySendAsync(ParentsTopic, forParents, cancellationToken);
        await TrySendAsync(GetChildTopic(child.Id), forChild, cancellationToken);
    }

    public async Task NotifyPointsRemovedAsync(
        string actorName,
        int childId,
        int chorePoints,
        string choreName,
        CancellationToken cancellationToken = default)
    {
        var message = $"{actorName} has removed {chorePoints} points {choreName}";
        await TrySendAsync(ParentsTopic, message, cancellationToken);
        await TrySendAsync(GetChildTopic(childId), message, cancellationToken);
    }

    public async Task SendMorningNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = dateTimeProvider.UtcNow;
        var isBreakpointDay = IsBreakpointDay(nowUtc);
        var period = isBreakpointDay
            ? GetPreviousPeriod(nowUtc)
            : monthlyCycleService.GetCurrentPeriod(nowUtc);

        var children = await childRepository.GetChildrenWithAssignmentsAsync();
        if (children.Count == 0)
            return;

        var childIds = children.Select(c => c.Id).ToArray();
        var monthlyPoints = await choreLogRepository.GetMonthlyPointsAsync(
            childIds,
            period.StartInclusive,
            period.EndExclusive);

        foreach (var child in children)
        {
            var totalPoints = monthlyPoints.GetValueOrDefault(child.Id, 0);
            var earnedAmount = CalculateEarnedAmount(child, totalPoints);
            var message = isBreakpointDay
                ? $"Time for Allowance! you have earned {earnedAmount}"
                : $"You have {totalPoints} you have earned {earnedAmount} of {child.MonthlyAllowance ?? 0}.";

            await TrySendAsync(GetChildTopic(child.Id), message, cancellationToken);
        }
    }

    private static string GetChildTopic(int childId) => $"child-{childId}";

    private async Task TrySendAsync(string topic, string message, CancellationToken cancellationToken)
    {
        try
        {
            await firebaseSender.SendToTopicAsync(topic, message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Firebase notification to topic {Topic}.", topic);
        }
    }

    private bool IsBreakpointDay(DateTimeOffset nowUtc)
    {
        var dayInMonth = Math.Min(
            monthlyCycleOptions.Value.BreakpointDay,
            DateTime.DaysInMonth(nowUtc.Year, nowUtc.Month));

        return nowUtc.Day == dayInMonth;
    }

    private MonthlyCyclePeriod GetPreviousPeriod(DateTimeOffset nowUtc)
    {
        var currentPeriod = monthlyCycleService.GetCurrentPeriod(nowUtc);
        var previousTick = currentPeriod.StartInclusive.AddTicks(-1);
        return monthlyCycleService.GetCurrentPeriod(previousTick);
    }

    private static decimal CalculateEarnedAmount(User child, int totalPoints)
    {
        if (child.MonthlyAllowance is null || child.PointsGoal is null || child.PointsGoal <= 0)
            return 0m;

        var safePoints = Math.Max(totalPoints, 0);
        var percentage = Math.Min(
            Math.Round((safePoints / (decimal)child.PointsGoal.Value) * 100m, MidpointRounding.AwayFromZero),
            100m);

        return Math.Ceiling((percentage / 100m) * child.MonthlyAllowance.Value);
    }
}
