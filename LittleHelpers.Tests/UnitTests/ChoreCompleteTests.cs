using System.Security.Claims;
using LittleHelpers.ApiService.Application.Chores;
using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Data;
using LittleHelpers.ApiService.Data.Repositories;
using LittleHelpers.ApiService.Models;
using LittleHelpers.ApiService.Services.Notifications;
using LittleHelpers.ApiService.Services;
using LittleHelpers.ApiService.Services.Realtime;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LittleHelpers.Tests.UnitTests;

public class ChoreCompleteTests : IDisposable
{
    private sealed class FakeDateTimeProvider(DateTimeOffset initialNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = initialNow;
        public DateTime UtcNowDateTime => UtcNow.UtcDateTime;
    }

    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CompleteChoreCommandHandler _handler;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly IChildRealtimeNotifier _realtimeNotifier;
    private readonly INotificationService _notificationService;

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
        _realtimeNotifier = Substitute.For<IChildRealtimeNotifier>();
        _notificationService = Substitute.For<INotificationService>();
        _dateTimeProvider = new FakeDateTimeProvider(new DateTimeOffset(2026, 6, 13, 10, 0, 0, TimeSpan.Zero));
        _handler = new CompleteChoreCommandHandler(
            new EfChoreRepository(_db),
            new EfChoreLogRepository(_db),
            new ChoreAvailabilityService(),
            _notificationService,
            _realtimeNotifier,
            _httpContextAccessor,
            _dateTimeProvider);
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
    }

    [Fact]
    public async Task Complete_AssignedChild_CreatesChoreLog()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        SetUser(_child.Id, "child", "Child");

        var result = await _handler.Handle(new CompleteChoreCommand(_chore.Id), cancellationToken);

        var log = _db.ChoreLogs.FirstOrDefault();
        Assert.NotNull(log);
        Assert.Equal(_chore.Id, log.ChoreId);
        Assert.Equal(_child.Id, log.ChildId);
        Assert.Equal(_child.Id, log.PerformedBy);
        Assert.Equal(10, log.Points);
        Assert.Equal(_chore.Id, result.ChoreId);
        Assert.Equal(_child.Id, result.ChildId);
        await _realtimeNotifier.Received(1).NotifyChildUpdatedAsync(_child.Id, "chore_completed", cancellationToken);
        await _notificationService.Received(1).NotifyPointsGivenAsync(
            "child",
            _child.Id,
            _chore.Points,
            _chore.Name,
            cancellationToken);
    }

    [Fact]
    public async Task Complete_UnassignedChild_ReturnsForbid()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var otherChild = new User { Username = "other", PasswordHash = "x", UserLevel = UserLevel.Child };
        _db.Users.Add(otherChild);
        await _db.SaveChangesAsync(cancellationToken);
        SetUser(otherChild.Id, "other", "Child");

        await Assert.ThrowsAsync<RequestAuthorizationException>(() =>
            _handler.Handle(new CompleteChoreCommand(_chore.Id), cancellationToken));
    }

    [Fact]
    public async Task Complete_Parent_UsesAssignedChildId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        SetUser(_parent.Id, "parent", "Parent");

        var result = await _handler.Handle(new CompleteChoreCommand(_chore.Id), cancellationToken);

        var log = _db.ChoreLogs.FirstOrDefault();
        Assert.NotNull(log);
        Assert.Equal(_child.Id, log.ChildId);
        Assert.Equal(_parent.Id, log.PerformedBy);
        Assert.Equal(_child.Id, result.ChildId);
        Assert.Equal(_parent.Id, result.PerformedBy);
    }

    [Fact]
    public async Task Complete_NonexistentChore_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        SetUser(_child.Id, "child", "Child");

        await Assert.ThrowsAsync<RequestNotFoundException>(() =>
            _handler.Handle(new CompleteChoreCommand(9999), cancellationToken));
    }

    [Fact]
    public async Task Complete_ExceedsMaxTimesPerDay_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var chore = new Chore { Name = "Daily task", Points = 5, MaxTimesPerDay = 1 };
        _db.Chores.Add(chore);
        _db.SaveChanges();
        _db.ChoreAssignments.Add(new ChoreAssignment { ChoreId = chore.Id, UserId = _child.Id });
        _db.SaveChanges();

        SetUser(_child.Id, "child", "Child");

        // First completion should succeed
        var result1 = await _handler.Handle(new CompleteChoreCommand(chore.Id), cancellationToken);
        Assert.Equal(chore.Id, result1.ChoreId);

        // Second completion same day should fail
        await Assert.ThrowsAsync<RequestValidationException>(() =>
            _handler.Handle(new CompleteChoreCommand(chore.Id), cancellationToken));
    }

    [Fact]
    public async Task Complete_AfterNextDay_AllowsCompletionAgain()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var chore = new Chore { Name = "Daily task", Points = 5, MaxTimesPerDay = 1 };
        _db.Chores.Add(chore);
        _db.SaveChanges();
        _db.ChoreAssignments.Add(new ChoreAssignment { ChoreId = chore.Id, UserId = _child.Id });
        _db.SaveChanges();

        SetUser(_child.Id, "child", "Child");

        await _handler.Handle(new CompleteChoreCommand(chore.Id), cancellationToken);
        _dateTimeProvider.UtcNow = _dateTimeProvider.UtcNow.AddDays(1);

        var result = await _handler.Handle(new CompleteChoreCommand(chore.Id), cancellationToken);
        Assert.Equal(chore.Id, result.ChoreId);
    }

    [Fact]
    public async Task Complete_ViolatesTooSoonForMinDaysBetween_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var chore = new Chore { Name = "Every other day", Points = 10, MinDaysBetween = 2 };
        _db.Chores.Add(chore);
        _db.SaveChanges();
        _db.ChoreAssignments.Add(new ChoreAssignment { ChoreId = chore.Id, UserId = _child.Id });
        _db.SaveChanges();

        SetUser(_child.Id, "child", "Child");

        // First completion
        await _handler.Handle(new CompleteChoreCommand(chore.Id), cancellationToken);

        // Try again immediately - should fail
        await Assert.ThrowsAsync<RequestValidationException>(() =>
            _handler.Handle(new CompleteChoreCommand(chore.Id), cancellationToken));
    }

    [Fact]
    public async Task Complete_ExceedsMaxTimesPerWeek_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var chore = new Chore { Name = "Weekly task", Points = 15, MaxTimesPerWeek = 1 };
        _db.Chores.Add(chore);
        _db.SaveChanges();
        _db.ChoreAssignments.Add(new ChoreAssignment { ChoreId = chore.Id, UserId = _child.Id });
        _db.SaveChanges();

        SetUser(_child.Id, "child", "Child");

        // First completion in the week
        await _handler.Handle(new CompleteChoreCommand(chore.Id), cancellationToken);

        // Second completion same week should fail
        await Assert.ThrowsAsync<RequestValidationException>(() =>
            _handler.Handle(new CompleteChoreCommand(chore.Id), cancellationToken));
    }

    [Fact]
    public async Task Complete_Parent_CanOverrideFrequencyLimits()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var chore = new Chore { Name = "Weekly task", Points = 15, MaxTimesPerWeek = 1 };
        _db.Chores.Add(chore);
        _db.SaveChanges();
        _db.ChoreAssignments.Add(new ChoreAssignment { ChoreId = chore.Id, UserId = _child.Id });
        _db.SaveChanges();

        SetUser(_child.Id, "child", "Child");
        await _handler.Handle(new CompleteChoreCommand(chore.Id), cancellationToken);

        SetUser(_parent.Id, "parent", "Parent");
        var result = await _handler.Handle(new CompleteChoreCommand(chore.Id, _child.Id), cancellationToken);

        Assert.Equal(chore.Id, result.ChoreId);
        Assert.Equal(_child.Id, result.ChildId);
        Assert.Equal(_parent.Id, result.PerformedBy);
    }

    public void Dispose() => _db.Dispose();
}
