using LittleHelpers.ApiService;
using LittleHelpers.ApiService.Data;
using LittleHelpers.ApiService.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LittleHelpers.Tests.ApiTests;

public class ApiFactory : WebApplicationFactory<ApiMarker>
{
    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtHelper.Key,
                ["Jwt:Issuer"] = JwtHelper.Issuer,
                ["Jwt:Audience"] = JwtHelper.Audience
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    public async Task SeedAsync(Func<AppDbContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await seed(db);
    }

    public HttpClient CreateAuthenticatedClient(int userId, string username, string role)
    {
        var client = CreateClient();
        var token = JwtHelper.GenerateToken(userId, username, role);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static User MakeParent(string username = "parent1") =>
        new() { Username = username, PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass123"), UserLevel = UserLevel.Parent };

    public static User MakeChild(string username = "child1") =>
        new() { Username = username, PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass123"), UserLevel = UserLevel.Child };
}
