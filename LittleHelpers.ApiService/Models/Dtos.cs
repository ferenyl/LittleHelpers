using LittleHelpers.ApiService.Models;
using LittleHelpers.ApiService.Services;

namespace LittleHelpers.ApiService.Models;

public record UserDto(int Id, string Username, string UserLevel, decimal? MonthlyAllowance, int? PointsGoal, IEnumerable<Link> Links);
public record CreateUserRequest(string Username, string Password, string UserLevel, decimal? MonthlyAllowance = null, int? PointsGoal = null);
public record UpdateUserRequest(string? Password, string? UserLevel, decimal? MonthlyAllowance = null, int? PointsGoal = null);

public record ChoreDto(int Id, string Name, int Points, bool IsHidden, int? MaxTimesPerDay, int? MinDaysBetween, int? MaxTimesPerWeek, IEnumerable<int> AssignedUserIds, IEnumerable<Link> Links, bool IsLimited = false);
public record CreateChoreRequest(string Name, int Points, List<int> AssignedUserIds, int? MaxTimesPerDay = null, int? MinDaysBetween = null, int? MaxTimesPerWeek = null);
public record UpdateChoreRequest(string? Name, int? Points, bool? IsHidden, List<int>? AssignedUserIds, int? MaxTimesPerDay = null, int? MinDaysBetween = null, int? MaxTimesPerWeek = null);

public record ChoreLogDto(int Id, int ChoreId, string ChoreName, int ChildId, int PerformedBy, string PerformedByName, int Points, DateTimeOffset Timestamp);
public record ChoreLogPeriodDto(
    IReadOnlyList<ChoreLogDto> Logs,
    DateTimeOffset PeriodStartInclusive,
    DateTimeOffset PeriodEndExclusive);

public record ChildSummaryDto(int Id, string Username, int TotalPoints, decimal? MonthlyAllowance, int? PointsGoal, IEnumerable<ChoreDto> AssignedChores, IEnumerable<Link> Links);

public record MenuItemDto(string Label, string Route);
