using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Data.Repositories;

public interface IChoreRepository
{
    Task<List<Chore>> GetChoresWithAssignmentsAsync();
    Task<Chore?> GetChoreWithAssignmentsAsync(int id);
    Task<Chore?> GetTrackedChoreWithAssignmentsAsync(int id);
    Task<List<Chore>> GetParentChoresWithAssignmentsAsync(IReadOnlyCollection<int> parentIds);
    Task AddAsync(Chore chore);
    Task ReplaceAssignmentsAsync(Chore chore, IReadOnlyCollection<int> userIds);
    void Remove(Chore chore);
    Task SaveChangesAsync();
}
