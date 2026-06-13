using LittleHelpers.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace LittleHelpers.ApiService.Data.Repositories;

public sealed class EfChildRepository(AppDbContext db) : IChildRepository
{
    public Task<List<User>> GetChildrenWithAssignmentsAsync() =>
        db.Users
            .AsNoTracking()
            .Where(u => u.UserLevel == UserLevel.Child)
            .Include(u => u.ChoreAssignments)
                .ThenInclude(ca => ca.Chore)
            .ToListAsync();

    public Task<User?> GetChildWithAssignmentsAsync(int id) =>
        db.Users
            .AsNoTracking()
            .Where(u => u.Id == id && u.UserLevel == UserLevel.Child)
            .Include(u => u.ChoreAssignments)
                .ThenInclude(ca => ca.Chore)
            .FirstOrDefaultAsync();

    public Task<List<int>> GetParentIdsAsync() =>
        db.Users
            .AsNoTracking()
            .Where(u => u.UserLevel == UserLevel.Parent)
            .Select(u => u.Id)
            .ToListAsync();
}
