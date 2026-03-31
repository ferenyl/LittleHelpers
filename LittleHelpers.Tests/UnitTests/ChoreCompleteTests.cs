using System.Security.Claims;
using LittleHelpers.ApiService.Controllers;
using LittleHelpers.ApiService.Data;
using LittleHelpers.ApiService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LittleHelpers.Tests.UnitTests;

public class ChoreCompleteTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ChoresController _controller;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly User _parent;
    private readonly User _child;
    private readonly Chore _chore;

    public ChoreCompleteTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ChoreComplete-{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);

        _parent = new User { Username = "parent", PasswordHash = "x", UserLevel = UserLevel.Parent };
        _child = new User { Username = "child", PasswordHash = "x", UserLevel = UserLevel.Child };
        _db.Users.AddRange(_parent, _child);
        _db.SaveChanges();

        _chore = new Chore { Name = "Wash dishes", Points = 10 };
        _db.Chores.Add(_chore);
        _db.SaveChanges();

        _db.ChoreAssignments.Add(new ChoreAssignment { ChoreId = _chore.Id, UserId = _child.Id });
        _db.SaveChanges();

        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _controller = new ChoresController(_db, _httpContextAccessor);
    }

    private void SetUser(int userId, string username, string role)
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        ], "test"));
        _httpContextAccessor.HttpContext.Returns(ctx);
        _controller.ControllerContext = new ControllerContext { HttpContext = ctx };
    }

    [Fact]
    public async Task Complete_AssignedChild_CreatesChoreLog()
    {
        SetUser(_child.Id, "child", "Child");

        await _controller.Complete(_chore.Id);

        var log = _db.ChoreLogs.FirstOrDefault();
        Assert.NotNull(log);
        Assert.Equal(_chore.Id, log.ChoreId);
        Assert.Equal(_child.Id, log.ChildId);
        Assert.Equal(_child.Id, log.PerformedBy);
        Assert.Equal(10, log.Points);
    }

    [Fact]
    public async Task Complete_UnassignedChild_ReturnsForbid()
    {
        var otherChild = new User { Username = "other", PasswordHash = "x", UserLevel = UserLevel.Child };
        _db.Users.Add(otherChild);
        await _db.SaveChangesAsync();
        SetUser(otherChild.Id, "other", "Child");

        var result = await _controller.Complete(_chore.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Complete_Parent_UsesAssignedChildId()
    {
        SetUser(_parent.Id, "parent", "Parent");

        await _controller.Complete(_chore.Id);

        var log = _db.ChoreLogs.FirstOrDefault();
        Assert.NotNull(log);
        Assert.Equal(_child.Id, log.ChildId);
        Assert.Equal(_parent.Id, log.PerformedBy);
    }

    [Fact]
    public async Task Complete_NonexistentChore_ReturnsNotFound()
    {
        SetUser(_child.Id, "child", "Child");

        var result = await _controller.Complete(9999);

        Assert.IsType<NotFoundResult>(result);
    }

    public void Dispose() => _db.Dispose();
}
