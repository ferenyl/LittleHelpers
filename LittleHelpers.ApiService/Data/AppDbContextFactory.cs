using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LittleHelpers.ApiService.Data;

/// <summary>
/// Design-time factory used by EF Core tools (dotnet ef migrations add/remove).
/// Connection string is read from the LITTLEHELPERS_CONNSTR environment variable.
/// Fallback: set env var before running migrations.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("LITTLEHELPERS_CONNSTR")
            ?? "Host=localhost;Database=littlehelpers;Username=postgres;Password=CHANGE_ME";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connStr)
            .Options;
        return new AppDbContext(options);
    }
}
