using LittleHelpers.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace LittleHelpers.ApiService.Data.Repositories;

public sealed class EfChoreRepository(AppDbContext db) : IChoreRepository
{
    public Task<List<Chore>> GetChoresWithAssignmentsAsync() =>
        db.Chores
            .AsNoTracking()
            .Include(c => c.ChoreAssignments)
            .ToListAsync();

    public Task<Chore?> GetChoreWithAssignmentsAsync(int id) =>
        db.Chores
            .AsNoTracking()
            .Include(c => c.ChoreAssignments)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<Chore?> GetTrackedChoreWithAssignmentsAsync(int id) =>
        db.Chores
            .Include(c => c.ChoreAssignments)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<List<Chore>> GetParentChoresWithAssignmentsAsync(IReadOnlyCollection<int> parentIds) =>
        db.Chores
            .AsNoTracking()
            .Include(c => c.ChoreAssignments)
            .Where(c => !c.IsHidden && c.ChoreAssignments.Any(a => parentIds.Contains(a.UserId)))
            .ToListAsync();

    public Task AddAsync(Chore chore)
    {
        db.Chores.Add(chore);
        return Task.CompletedTask;
    }

    public Task ReplaceAssignmentsAsync(Chore chore, IReadOnlyCollection<int> userIds)
    {
        db.ChoreAssignments.RemoveRange(chore.ChoreAssignments);
        chore.ChoreAssignments.Clear();
        foreach (var userId in userIds)
            db.ChoreAssignments.Add(new ChoreAssignment { ChoreId = chore.Id, UserId = userId });
        return Task.CompletedTask;
    }

    public void Remove(Chore chore) => db.Chores.Remove(chore);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
