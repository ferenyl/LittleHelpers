using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Data.Repositories;

public interface IChildRepository
{
    Task<List<User>> GetChildrenWithAssignmentsAsync();
    Task<User?> GetChildWithAssignmentsAsync(int id);
    Task<List<int>> GetParentIdsAsync();
}
