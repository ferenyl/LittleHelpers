using System.Net;
using System.Net.Http.Json;
using LittleHelpers.ApiService.Models;

namespace LittleHelpers.Tests.ApiTests;

public class ChildrenApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ChildrenApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetChildren_AsParent_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.GetAsync("/children");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetChildren_AsChild_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(99, "child", "Child");
        var response = await client.GetAsync("/children");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetChildDetail_AsParent_ReturnsChildWithChores()
    {
        int childId = 0, choreId = 0;
        await _factory.SeedAsync(async db =>
        {
            var child = new User
            {
                Username = $"detail_child_{Guid.NewGuid():N}", PasswordHash = "x",
                UserLevel = UserLevel.Child
            };
            db.Users.Add(child);
            var chore = new Chore { Name = "Detail chore", Points = 5 };
            db.Chores.Add(chore);
            await db.SaveChangesAsync();
            childId = child.Id;
            choreId = chore.Id;
            db.ChoreAssignments.Add(new() { ChoreId = choreId, UserId = childId });
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.GetAsync($"/children/{childId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChildSummaryDto>();
        Assert.Equal(childId, body?.Id);
        Assert.NotNull(body?.AssignedChores);
        Assert.Contains(body!.AssignedChores, c => c.Id == choreId);
    }

    [Fact]
    public async Task GetChildDetail_AsOwnChild_ReturnsOk()
    {
        int childId = 0;
        await _factory.SeedAsync(async db =>
        {
            var child = new User
            {
                Username = $"self_child_{Guid.NewGuid():N}", PasswordHash = "x",
                UserLevel = UserLevel.Child
            };
            db.Users.Add(child);
            await db.SaveChangesAsync();
            childId = child.Id;
        });

        var client = _factory.CreateAuthenticatedClient(childId, "self_child", "Child");
        var response = await client.GetAsync($"/children/{childId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetChildDetail_NonExistent_ReturnsNotFound()
    {
        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.GetAsync("/children/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetChoreLog_AsParent_ReturnsOk()
    {
        int childId = 0;
        await _factory.SeedAsync(async db =>
        {
            var child = new User
            {
                Username = $"log_child_{Guid.NewGuid():N}", PasswordHash = "x",
                UserLevel = UserLevel.Child
            };
            db.Users.Add(child);
            await db.SaveChangesAsync();
            childId = child.Id;
            db.ChoreLogs.Add(new ChoreLog
            {
                ChoreId = 1, ChoreName = "Cleaned", ChildId = childId,
                PerformedBy = childId, Points = 5, Timestamp = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.GetAsync($"/chorelog/{childId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<IEnumerable<ChoreLogDto>>();
        Assert.Contains(logs!, l => l.ChildId == childId);
    }

    [Fact]
    public async Task GetChoreLog_WithMonthFilter_ReturnsFilteredLogs()
    {
        int childId = 0;
        await _factory.SeedAsync(async db =>
        {
            var child = new User
            {
                Username = $"filter_child_{Guid.NewGuid():N}", PasswordHash = "x",
                UserLevel = UserLevel.Child
            };
            db.Users.Add(child);
            await db.SaveChangesAsync();
            childId = child.Id;
            db.ChoreLogs.AddRange(
                new ChoreLog { ChoreId = 1, ChoreName = "Jan chore", ChildId = childId, PerformedBy = childId, Points = 3, Timestamp = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero) },
                new ChoreLog { ChoreId = 1, ChoreName = "Feb chore", ChildId = childId, PerformedBy = childId, Points = 7, Timestamp = new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero) }
            );
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateAuthenticatedClient(1, "admin", "Parent");
        var response = await client.GetAsync($"/chorelog/{childId}?year=2025&month=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<IEnumerable<ChoreLogDto>>();
        Assert.All(logs!, l => Assert.Equal(2025, l.Timestamp.Year));
        Assert.All(logs!, l => Assert.Equal(1, l.Timestamp.Month));
    }
}
