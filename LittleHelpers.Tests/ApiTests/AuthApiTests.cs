using System.Net;
using System.Net.Http.Json;
using LittleHelpers.ApiService.Models;

namespace LittleHelpers.Tests.ApiTests;

public class AuthApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        await _factory.SeedAsync(async db =>
        {
            db.Users.Add(ApiFactory.MakeParent("auth_parent"));
            await db.SaveChangesAsync();
        });

        var response = await _client.PostAsJsonAsync("/auth/login",
            new { Username = "auth_parent", Password = "pass123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body?.Token);
        Assert.Equal("auth_parent", body!.Username);
        Assert.Equal("Parent", body.UserLevel);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await _factory.SeedAsync(async db =>
        {
            db.Users.Add(ApiFactory.MakeParent("auth_parent2"));
            await db.SaveChangesAsync();
        });

        var response = await _client.PostAsJsonAsync("/auth/login",
            new { Username = "auth_parent2", Password = "wrongpassword" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/auth/login",
            new { Username = "does_not_exist", Password = "any" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
