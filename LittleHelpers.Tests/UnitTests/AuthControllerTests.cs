using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LittleHelpers.ApiService.Application.Auth;
using LittleHelpers.ApiService.Application.Cqrs;
using LittleHelpers.ApiService.Data;
using LittleHelpers.ApiService.Data.Repositories;
using LittleHelpers.ApiService.Models;
using LittleHelpers.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LittleHelpers.Tests.UnitTests;

public class AuthControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly LoginQueryHandler _loginHandler;
    private readonly RenewTokenQueryHandler _renewTokenHandler;
    private readonly int _userId;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AuthTests-{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-super-secret-jwt-key-that-is-long-enough-32chars!",
                ["Jwt:Issuer"] = "LittleHelpers",
                ["Jwt:Audience"] = "LittleHelpers",
                ["Jwt:AccessTokenLifetimeHours"] = "168",
                ["Jwt:RenewTokenLifetimeHours"] = "336"
            })
            .Build();

        var userRepository = new EfUserRepository(_db);
        var tokenFactory = new JwtTokenFactory(config, new SystemDateTimeProvider());
        _loginHandler = new LoginQueryHandler(userRepository, tokenFactory);
        _renewTokenHandler = new RenewTokenQueryHandler(userRepository, tokenFactory);

        var user = new User
        {
            Username = "parent1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
            UserLevel = UserLevel.Parent
        };
        _db.Users.Add(user);
        _db.SaveChanges();
        _userId = user.Id;
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        var response = await _loginHandler.Handle(
            new LoginQuery("parent1", "correct-password"),
            TestContext.Current.CancellationToken);
        Assert.NotEmpty(response.Token);
        Assert.Equal("parent1", response.Username);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await Assert.ThrowsAsync<RequestAuthenticationException>(() =>
            _loginHandler.Handle(new LoginQuery("parent1", "wrong-password"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Login_UnknownUsername_ReturnsUnauthorized()
    {
        await Assert.ThrowsAsync<RequestAuthenticationException>(() =>
            _loginHandler.Handle(new LoginQuery("nobody", "any-password"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Login_ValidCredentials_TokenContainsCorrectClaims()
    {
        var response = await _loginHandler.Handle(
            new LoginQuery("parent1", "correct-password"),
            TestContext.Current.CancellationToken);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.Token);

        Assert.Equal("parent1", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("Parent", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public async Task Login_UsesConfiguredAccessTokenLifetimeHours()
    {
        var response = await _loginHandler.Handle(
            new LoginQuery("parent1", "correct-password"),
            TestContext.Current.CancellationToken);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
        var remainingHours = (jwt.ValidTo - DateTime.UtcNow).TotalHours;

        Assert.InRange(remainingHours, 167.5, 168.1);
    }

    [Fact]
    public async Task Renew_UsesConfiguredRenewTokenLifetimeHours()
    {
        var response = await _renewTokenHandler.Handle(
            new RenewTokenQuery(_userId),
            TestContext.Current.CancellationToken);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
        var remainingHours = (jwt.ValidTo - DateTime.UtcNow).TotalHours;

        Assert.InRange(remainingHours, 335.5, 336.1);
    }

    public void Dispose() => _db.Dispose();
}
