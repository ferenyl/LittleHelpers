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
    private readonly LoginQueryHandler _handler;

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
                ["Jwt:Audience"] = "LittleHelpers"
            })
            .Build();

        _handler = new LoginQueryHandler(new EfUserRepository(_db), config, new SystemDateTimeProvider());

        _db.Users.Add(new User
        {
            Username = "parent1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
            UserLevel = UserLevel.Parent
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        var response = await _handler.Handle(
            new LoginQuery("parent1", "correct-password"),
            TestContext.Current.CancellationToken);
        Assert.NotEmpty(response.Token);
        Assert.Equal("parent1", response.Username);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await Assert.ThrowsAsync<RequestAuthenticationException>(() =>
            _handler.Handle(new LoginQuery("parent1", "wrong-password"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Login_UnknownUsername_ReturnsUnauthorized()
    {
        await Assert.ThrowsAsync<RequestAuthenticationException>(() =>
            _handler.Handle(new LoginQuery("nobody", "any-password"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Login_ValidCredentials_TokenContainsCorrectClaims()
    {
        var response = await _handler.Handle(
            new LoginQuery("parent1", "correct-password"),
            TestContext.Current.CancellationToken);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.Token);

        Assert.Equal("parent1", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("Parent", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    public void Dispose() => _db.Dispose();
}
