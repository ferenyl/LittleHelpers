using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Data.Repositories;

public interface IChoreLogRepository
{
    Task<Dictionary<int, int>> GetMonthlyPointsAsync(IReadOnlyCollection<int> childIds, DateTimeOffset now);
    Task<List<ChoreLog>> GetLogsForChildAndChoreAsync(int childId, int choreId);
    Task<List<ChoreLogDto>> GetForChildAsync(int childId, int year, int month);
    Task<ChoreLog?> GetTrackedByIdAsync(int id);
    Task AddAsync(ChoreLog log);
    void Remove(ChoreLog log);
    Task SaveChangesAsync();
}
