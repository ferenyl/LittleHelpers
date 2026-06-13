using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Services;

public interface IChoreAvailabilityService
{
    string? GetBlockReason(Chore chore, IReadOnlyList<ChoreLog> logs, DateTimeOffset now);
    bool IsCompletable(Chore chore, IReadOnlyList<ChoreLog> logs, DateTimeOffset now) => GetBlockReason(chore, logs, now) is null;
}

public sealed class ChoreAvailabilityService : IChoreAvailabilityService
{
    public string? GetBlockReason(Chore chore, IReadOnlyList<ChoreLog> logs, DateTimeOffset now)
    {
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        var choreLogs = logs.Where(l => l.ChoreId == chore.Id).OrderByDescending(l => l.Timestamp).ToList();

        if (chore.MaxTimesPerDay.HasValue)
        {
            var timesToday = choreLogs.Count(l => l.Timestamp.Date == today);
            if (timesToday >= chore.MaxTimesPerDay.Value)
                return $"Kan inte göra denna syssla mer än {chore.MaxTimesPerDay} gång(er) per dag.";
        }

        if (chore.MinDaysBetween.HasValue && choreLogs.Any())
        {
            var lastLog = choreLogs.First();
            var daysSinceLast = (today - lastLog.Timestamp.Date).Days;
            if (daysSinceLast < chore.MinDaysBetween.Value)
                return $"Måste vänta minst {chore.MinDaysBetween} dag(ar) mellan genomföranden. Senast gjord: {lastLog.Timestamp.Date:yyyy-MM-dd}.";
        }

        if (chore.MaxTimesPerWeek.HasValue)
        {
            var timesThisWeek = choreLogs.Count(l => l.Timestamp.Date >= weekStart);
            if (timesThisWeek >= chore.MaxTimesPerWeek.Value)
                return $"Kan inte göra denna syssla mer än {chore.MaxTimesPerWeek} gång(er) per vecka.";
        }

        return null;
    }
}
