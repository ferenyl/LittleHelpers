using LittleHelpers.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace LittleHelpers.ApiService.Data.Repositories;

public sealed class EfUserRepository(AppDbContext db) : IUserRepository
{
    public Task<List<User>> GetAllAsync() =>
        db.Users.AsNoTracking().ToListAsync();

    public Task<User?> GetByIdAsync(int id) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetTrackedByIdAsync(int id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByUsernameAsync(string username) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);

    public Task AddAsync(User user)
    {
        db.Users.Add(user);
        return Task.CompletedTask;
    }

    public void Remove(User user) => db.Users.Remove(user);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
