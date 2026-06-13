using LittleHelpers.ApiService.Application.Cqrs;

namespace LittleHelpers.ApiService.Application.Users;

[RequireRoles("Parent")]
public sealed record GetUserByIdQuery(int UserId);

public sealed class GetUserByIdQueryHandler(
    IUserRepository userRepository,
    IHttpContextAccessor httpContext) : IQueryHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(request.UserId)
            ?? throw new RequestNotFoundException("Användaren kunde inte hittas.");

        var lw = new LinkWriter<UserDto>(httpContext)
            .AddLink("self", "GET", u => $"/users/{u.Id}")
            .AddLinkForRole("Parent", "edit", "PUT", u => $"/users/{u.Id}")
            .AddLinkForRole("Parent", "delete", "DELETE", u => $"/users/{u.Id}");

        var dto = DtoFactory.CreateUserDto(user);
        return dto with { Links = lw.Build(dto) };
    }
}
