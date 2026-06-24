using LittleHelpers.ApiService.Application.Cqrs;

namespace LittleHelpers.ApiService.Application.Auth;

public sealed record LoginQuery(string Username, string Password);

public sealed class LoginQueryHandler(
    IUserRepository userRepository,
    JwtTokenFactory jwtTokenFactory) : IQueryHandler<LoginQuery, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginQuery request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByUsernameAsync(request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new RequestAuthenticationException("Invalid username or password.");

        return new LoginResponse(
            jwtTokenFactory.CreateAccessToken(user),
            user.Username,
            user.UserLevel.ToString());
    }
}
