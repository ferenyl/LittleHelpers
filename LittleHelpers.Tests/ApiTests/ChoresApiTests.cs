using System.Net;
using System.Net.Http.Json;
using LittleHelpers.ApiService.Models;

namespace LittleHelpers.Tests.ApiTests;

public class ChoresApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ChoresApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetChores_AsParent_ReturnsOk()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.GetAsync("/chores", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetChores_AsChild_ReturnsForbidden()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateAuthenticatedClient(99, "child", "Child");
        var response = await client.GetAsync("/chores", cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateChore_AsParent_ReturnsCreated()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.PostAsJsonAsync("/chores",
            new { Name = "Sweep floor", Points = 5, AssignedUserIds = new int[] { } },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChoreDto>(cancellationToken);
        Assert.Equal("Sweep floor", body?.Name);
        Assert.Equal(5, body?.Points);
    }

    [Fact]
    public async Task CreateChore_WithAssignments_ReturnsAssignedUserIds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        int childId = 0;
        await _factory.SeedAsync(async db =>
        {
            var user = new LittleHelpers.ApiService.Models.User
            {
                Username = $"chore_child_{Guid.NewGuid():N}",
                PasswordHash = "x",
                UserLevel = LittleHelpers.ApiService.Models.UserLevel.Child
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            childId = user.Id;
        });

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.PostAsJsonAsync("/chores",
            new { Name = "Clean room", Points = 15, AssignedUserIds = new[] { childId } },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChoreDto>(cancellationToken);
        Assert.Contains(childId, body?.AssignedUserIds ?? []);
    }

    [Fact]
    public async Task UpdateChore_AsParent_ReturnsOk()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        int choreId = await CreateChoreAndGetId("Chore to update", 10);

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.PutAsJsonAsync($"/chores/{choreId}",
            new { Name = "Updated chore", Points = 20 },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteChore_AsParent_ReturnsNoContent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        int choreId = await CreateChoreAndGetId("Chore to delete", 5);

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.DeleteAsync($"/chores/{choreId}", cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CompleteChore_AssignedChild_ReturnsOkWithLog()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        int childId = 0, choreId = 0;
        await _factory.SeedAsync(async db =>
        {
            var child = new LittleHelpers.ApiService.Models.User
            {
                Username = $"complete_child_{Guid.NewGuid():N}", PasswordHash = "x",
                UserLevel = LittleHelpers.ApiService.Models.UserLevel.Child
            };
            db.Users.Add(child);
            var chore = new LittleHelpers.ApiService.Models.Chore { Name = "Assigned chore", Points = 8 };
            db.Chores.Add(chore);
            await db.SaveChangesAsync(cancellationToken);
            childId = child.Id;
            choreId = chore.Id;
            db.ChoreAssignments.Add(new() { ChoreId = choreId, UserId = childId });
            await db.SaveChangesAsync(cancellationToken);
        });

        var client = _factory.CreateAuthenticatedClient(childId, "complete_child", "Child");
        var response = await client.PostAsync($"/chores/{choreId}/complete", null, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var log = await response.Content.ReadFromJsonAsync<ChoreLogDto>(cancellationToken);
        Assert.Equal(choreId, log?.ChoreId);
        Assert.Equal(8, log?.Points);
    }

    [Fact]
    public async Task CompleteChore_UnassignedChild_ReturnsForbidden()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        int choreId = 0;
        await _factory.SeedAsync(async db =>
        {
            var chore = new LittleHelpers.ApiService.Models.Chore { Name = "Other chore", Points = 5 };
            db.Chores.Add(chore);
            await db.SaveChangesAsync(cancellationToken);
            choreId = chore.Id;
        });

        var client = _factory.CreateAuthenticatedClient(99, "stranger_child", "Child");
        var response = await client.PostAsync($"/chores/{choreId}/complete", null, cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<int> CreateChoreAndGetId(string name, int points)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.PostAsJsonAsync("/chores",
            new { Name = name, Points = points, AssignedUserIds = new int[] { } },
            cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ChoreDto>(cancellationToken);
        return body!.Id;
    }
}
