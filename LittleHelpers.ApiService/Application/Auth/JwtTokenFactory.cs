using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LittleHelpers.ApiService.Application.Auth;

public sealed class JwtTokenFactory(
    IConfiguration config,
    IDateTimeProvider dateTimeProvider)
{
    private const int DefaultAccessTokenLifetimeHours = 24 * 7;
    private const int DefaultRenewTokenLifetimeHours = 24 * 14;

    public string CreateAccessToken(User user)
        => CreateToken(user, ResolveLifetimeHours("Jwt:AccessTokenLifetimeHours", DefaultAccessTokenLifetimeHours));

    public string CreateRenewedToken(User user)
        => CreateToken(user, ResolveLifetimeHours("Jwt:RenewTokenLifetimeHours", DefaultRenewTokenLifetimeHours));

    private string CreateToken(User user, int lifetimeHours)
    {
        var jwtKey = config["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key is not configured.");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.UserLevel.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: dateTimeProvider.UtcNowDateTime.AddHours(lifetimeHours),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int ResolveLifetimeHours(string configKey, int fallbackHours)
    {
        var configuredValue = config[configKey];
        if (string.IsNullOrWhiteSpace(configuredValue))
            return fallbackHours;

        if (!int.TryParse(configuredValue, out var hours) || hours <= 0)
            throw new InvalidOperationException(
                $"Invalid value for '{configKey}'. Configure a positive number of hours.");

        return hours;
    }
}
