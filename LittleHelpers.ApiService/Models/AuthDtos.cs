namespace LittleHelpers.ApiService.Models;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, string UserLevel);
public record RenewTokenResponse(string Token);
