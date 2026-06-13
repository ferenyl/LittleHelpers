using LittleHelpers.ApiService.Models;

namespace LittleHelpers.ApiService.Data.Repositories;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetTrackedByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task AddAsync(User user);
    void Remove(User user);
    Task SaveChangesAsync();
}
