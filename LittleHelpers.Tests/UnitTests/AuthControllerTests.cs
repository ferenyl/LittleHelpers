using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LittleHelpers.ApiService.Controllers;
using LittleHelpers.ApiService.Data;
using LittleHelpers.ApiService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LittleHelpers.Tests.UnitTests;

public class AuthControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AuthController _controller;

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

        _controller = new AuthController(_db, config);

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
        var result = await _controller.Login(new("parent1", "correct-password"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<dynamic>(ok.Value);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var result = await _controller.Login(new("parent1", "wrong-password"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_UnknownUsername_ReturnsUnauthorized()
    {
        var result = await _controller.Login(new("nobody", "any-password"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_TokenContainsCorrectClaims()
    {
        var result = await _controller.Login(new("parent1", "correct-password"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = (LittleHelpers.ApiService.Models.LoginResponse)ok.Value!;

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.Token);

        Assert.Equal("parent1", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("Parent", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    public void Dispose() => _db.Dispose();
}
