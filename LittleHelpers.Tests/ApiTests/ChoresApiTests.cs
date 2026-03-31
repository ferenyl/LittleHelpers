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
        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.GetAsync("/chores");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetChores_AsChild_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(99, "child", "Child");
        var response = await client.GetAsync("/chores");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateChore_AsParent_ReturnsCreated()
    {
        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.PostAsJsonAsync("/chores",
            new { Name = "Sweep floor", Points = 5, AssignedUserIds = new int[] { } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChoreDto>();
        Assert.Equal("Sweep floor", body?.Name);
        Assert.Equal(5, body?.Points);
    }

    [Fact]
    public async Task CreateChore_WithAssignments_ReturnsAssignedUserIds()
    {
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
            await db.SaveChangesAsync();
            childId = user.Id;
        });

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.PostAsJsonAsync("/chores",
            new { Name = "Clean room", Points = 15, AssignedUserIds = new[] { childId } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChoreDto>();
        Assert.Contains(childId, body?.AssignedUserIds ?? []);
    }

    [Fact]
    public async Task UpdateChore_AsParent_ReturnsOk()
    {
        int choreId = await CreateChoreAndGetId("Chore to update", 10);

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.PutAsJsonAsync($"/chores/{choreId}",
            new { Name = "Updated chore", Points = 20 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteChore_AsParent_ReturnsNoContent()
    {
        int choreId = await CreateChoreAndGetId("Chore to delete", 5);

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.DeleteAsync($"/chores/{choreId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CompleteChore_AssignedChild_ReturnsOkWithLog()
    {
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
            await db.SaveChangesAsync();
            childId = child.Id;
            choreId = chore.Id;
            db.ChoreAssignments.Add(new() { ChoreId = choreId, UserId = childId });
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateAuthenticatedClient(childId, "complete_child", "Child");
        var response = await client.PostAsync($"/chores/{choreId}/complete", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var log = await response.Content.ReadFromJsonAsync<ChoreLogDto>();
        Assert.Equal(choreId, log?.ChoreId);
        Assert.Equal(8, log?.Points);
    }

    [Fact]
    public async Task CompleteChore_UnassignedChild_ReturnsForbidden()
    {
        int choreId = 0;
        await _factory.SeedAsync(async db =>
        {
            var chore = new LittleHelpers.ApiService.Models.Chore { Name = "Other chore", Points = 5 };
            db.Chores.Add(chore);
            await db.SaveChangesAsync();
            choreId = chore.Id;
        });

        var client = _factory.CreateAuthenticatedClient(99, "stranger_child", "Child");
        var response = await client.PostAsync($"/chores/{choreId}/complete", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<int> CreateChoreAndGetId(string name, int points)
    {
        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.PostAsJsonAsync("/chores",
            new { Name = name, Points = points, AssignedUserIds = new int[] { } });
        var body = await response.Content.ReadFromJsonAsync<ChoreDto>();
        return body!.Id;
    }
}
