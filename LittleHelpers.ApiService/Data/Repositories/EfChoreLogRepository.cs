using LittleHelpers.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace LittleHelpers.ApiService.Data.Repositories;

public sealed class EfChoreLogRepository(AppDbContext db) : IChoreLogRepository
{
    public Task<Dictionary<int, int>> GetMonthlyPointsAsync(IReadOnlyCollection<int> childIds, DateTimeOffset now) =>
        db.ChoreLogs
            .AsNoTracking()
            .Where(l => childIds.Contains(l.ChildId) && l.Timestamp.Year == now.Year && l.Timestamp.Month == now.Month)
            .GroupBy(l => l.ChildId)
            .Select(g => new { ChildId = g.Key, Points = g.Sum(l => l.Points) })
            .ToDictionaryAsync(x => x.ChildId, x => x.Points);

    public Task<List<ChoreLog>> GetLogsForChildAndChoreAsync(int childId, int choreId) =>
        db.ChoreLogs
            .AsNoTracking()
            .Where(l => l.ChildId == childId && l.ChoreId == choreId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();

    public Task<List<ChoreLogDto>> GetForChildAsync(int childId, int year, int month) =>
        db.ChoreLogs
            .AsNoTracking()
            .Where(l => l.ChildId == childId && l.Timestamp.Year == year && l.Timestamp.Month == month)
            .OrderByDescending(l => l.Timestamp)
            .Join(db.Users.AsNoTracking(),
                l => l.PerformedBy,
                u => u.Id,
                (l, u) => new ChoreLogDto(l.Id, l.ChoreId, l.ChoreName, l.ChildId, l.PerformedBy, u.Username, l.Points, l.Timestamp))
            .ToListAsync();

    public Task<ChoreLog?> GetTrackedByIdAsync(int id) =>
        db.ChoreLogs
            .FirstOrDefaultAsync(l => l.Id == id);

    public Task AddAsync(ChoreLog log)
    {
        db.ChoreLogs.Add(log);
        return Task.CompletedTask;
    }

    public void Remove(ChoreLog log) => db.ChoreLogs.Remove(log);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
