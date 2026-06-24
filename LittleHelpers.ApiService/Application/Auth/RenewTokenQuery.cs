namespace LittleHelpers.ApiService.Application.Auth;

public sealed record RenewTokenQuery(int UserId);

public sealed class RenewTokenQueryHandler(
    IUserRepository userRepository,
    JwtTokenFactory jwtTokenFactory) : IQueryHandler<RenewTokenQuery, RenewTokenResponse>
{
    public async Task<RenewTokenResponse> Handle(RenewTokenQuery request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(request.UserId)
            ?? throw new RequestAuthenticationException("Unable to renew token for unknown user.");

        return new RenewTokenResponse(jwtTokenFactory.CreateRenewedToken(user));
    }
}
