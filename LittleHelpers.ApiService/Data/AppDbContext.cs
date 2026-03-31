using LittleHelpers.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace LittleHelpers.ApiService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Chore> Chores => Set<Chore>();
    public DbSet<ChoreAssignment> ChoreAssignments => Set<ChoreAssignment>();
    public DbSet<ChoreLog> ChoreLogs => Set<ChoreLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChoreAssignment>()
            .HasKey(ca => new { ca.ChoreId, ca.UserId });

        modelBuilder.Entity<ChoreAssignment>()
            .HasOne(ca => ca.Chore)
            .WithMany(c => c.ChoreAssignments)
            .HasForeignKey(ca => ca.ChoreId);

        modelBuilder.Entity<ChoreAssignment>()
            .HasOne(ca => ca.User)
            .WithMany(u => u.ChoreAssignments)
            .HasForeignKey(ca => ca.UserId);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.UserLevel)
            .HasConversion<string>();
    }
}
