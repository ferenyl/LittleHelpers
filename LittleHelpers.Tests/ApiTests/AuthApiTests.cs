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
        var cancellationToken = TestContext.Current.CancellationToken;
        await _factory.SeedAsync(async db =>
        {
            db.Users.Add(ApiFactory.MakeParent("auth_parent"));
            await db.SaveChangesAsync(cancellationToken);
        });

        var response = await _client.PostAsJsonAsync("/auth/login",
            new { Username = "auth_parent", Password = "pass123" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
        Assert.NotNull(body?.Token);
        Assert.Equal("auth_parent", body!.Username);
        Assert.Equal("Parent", body.UserLevel);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _factory.SeedAsync(async db =>
        {
            db.Users.Add(ApiFactory.MakeParent("auth_parent2"));
            await db.SaveChangesAsync(cancellationToken);
        });

        var response = await _client.PostAsJsonAsync("/auth/login",
            new { Username = "auth_parent2", Password = "wrongpassword" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = await _client.PostAsJsonAsync("/auth/login",
            new { Username = "does_not_exist", Password = "any" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/users", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Renew_WithValidToken_ReturnsNewToken()
    {
        HttpClient authedClient = null!;
        var cancellationToken = TestContext.Current.CancellationToken;

        await _factory.SeedAsync(async db =>
        {
            var user = ApiFactory.MakeParent("renew_parent");
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            authedClient = _factory.CreateAuthenticatedClient(user.Id, user.Username, "Parent");
        });

        var response = await authedClient.PostAsync("/auth/renew", content: null, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RenewTokenResponse>(cancellationToken);
        Assert.NotNull(body?.Token);
    }
}
