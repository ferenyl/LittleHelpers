using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Application.Mapping;

public static class DtoFactory
{
    public static UserDto CreateUserDto(User user) =>
        new(user.Id, user.Username, user.UserLevel.ToString(), user.MonthlyAllowance, user.PointsGoal, []);

    public static ChoreDto CreateChoreDto(Chore chore) =>
        new(chore.Id, chore.Name, chore.Points, chore.IsHidden, chore.MaxTimesPerDay, chore.MinDaysBetween, chore.MaxTimesPerWeek,
            chore.ChoreAssignments.Select(a => a.UserId), []);

    public static ChildSummaryDto CreateChildSummaryDto(User user, IEnumerable<ChoreDto> chores, int totalPoints = 0) =>
        new(user.Id, user.Username, totalPoints, user.MonthlyAllowance, user.PointsGoal, chores, []);

    public static ChoreLogDto CreateChoreLogDto(ChoreLog log, string performedByName) =>
        new(log.Id, log.ChoreId, log.ChoreName, log.ChildId, log.PerformedBy, performedByName, log.Points, log.Timestamp);
}
