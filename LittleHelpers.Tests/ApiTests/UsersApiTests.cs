using System.Net;
using System.Net.Http.Json;
using LittleHelpers.ApiService.Models;

namespace LittleHelpers.Tests.ApiTests;

public class UsersApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UsersApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUsers_AsParent_ReturnsOk()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _factory.SeedAsync(async db =>
        {
            db.Users.Add(ApiFactory.MakeParent("users_parent1"));
            await db.SaveChangesAsync(cancellationToken);
        });

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.GetAsync("/users", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AsChild_ReturnsForbidden()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateAuthenticatedClient(99, "child", "Child");
        var response = await client.GetAsync("/users", cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_AsParent_ReturnsCreated()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.PostAsJsonAsync("/users",
            new { Username = "newchild", Password = "secret123", UserLevel = "Child" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
        Assert.Equal("newchild", body?.Username);
        Assert.Equal("Child", body?.UserLevel);
    }

    [Fact]
    public async Task CreateUser_AsChild_ReturnsForbidden()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateAuthenticatedClient(99, "child", "Child");
        var response = await client.PostAsJsonAsync("/users",
            new { Username = "x", Password = "y", UserLevel = "Child" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_AsParent_ReturnsOk()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        int userId = 0;
        await _factory.SeedAsync(async db =>
        {
            var user = new LittleHelpers.ApiService.Models.User
            {
                Username = $"edit_target_{Guid.NewGuid():N}",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("old"),
                UserLevel = LittleHelpers.ApiService.Models.UserLevel.Child
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            userId = user.Id;
        });

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.PutAsJsonAsync($"/users/{userId}",
            new { Password = "newpass", UserLevel = "Parent" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_AsParent_ReturnsNoContent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        int userId = 0;
        await _factory.SeedAsync(async db =>
        {
            var user = new LittleHelpers.ApiService.Models.User
            {
                Username = $"delete_target_{Guid.NewGuid():N}",
                PasswordHash = "x",
                UserLevel = LittleHelpers.ApiService.Models.UserLevel.Child
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            userId = user.Id;
        });

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.DeleteAsync($"/users/{userId}", cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_NonExistent_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.GetAsync("/users/99999", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
