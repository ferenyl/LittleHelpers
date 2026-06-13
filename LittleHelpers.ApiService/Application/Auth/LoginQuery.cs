using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LittleHelpers.ApiService.Application.Cqrs;
using Microsoft.IdentityModel.Tokens;

namespace LittleHelpers.ApiService.Application.Auth;

public sealed record LoginQuery(string Username, string Password);

public sealed class LoginQueryHandler(
    IUserRepository userRepository,
    IConfiguration config,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<LoginQuery, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginQuery request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByUsernameAsync(request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new RequestAuthenticationException("Invalid username or password.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

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
            expires: dateTimeProvider.UtcNowDateTime.AddHours(8),
            signingCredentials: creds);

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Username, user.UserLevel.ToString());
    }
}
