using System.Security.Claims;
using System.Reflection;

namespace LittleHelpers.ApiService.Application.Cqrs;

public sealed class RequestAuthorizationDecorator<TRequest, TResult>(
    IRequestHandler<TRequest, TResult> inner,
    IHttpContextAccessor httpContextAccessor) : IQueryHandler<TRequest, TResult>, ICommandHandler<TRequest, TResult>
    where TRequest : notnull
{
    public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User
            ?? throw new RequestAuthorizationException("Missing authenticated user.");

        var roles = request.GetType().GetCustomAttribute<RequireRolesAttribute>()?.Roles;
        if (roles is not null && roles.Count > 0 && !roles.Any(user.IsInRole))
            throw new RequestAuthorizationException("User is not allowed to execute this request.");

        if (request is IOwnedByCurrentUserRequest owned && user.IsInRole("Child"))
        {
            var currentUserId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (owned.OwnerUserId != currentUserId)
                throw new RequestAuthorizationException("Child users may only access their own data.");
        }

        return await inner.Handle(request, cancellationToken);
    }
}
