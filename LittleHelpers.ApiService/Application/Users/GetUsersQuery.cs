using LittleHelpers.ApiService.Application.Cqrs;

namespace LittleHelpers.ApiService.Application.Users;

[RequireRoles("Parent")]
public sealed record GetUsersQuery;

public sealed class GetUsersQueryHandler(
    IUserRepository userRepository,
    IHttpContextAccessor httpContext) : IQueryHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync();
        var lw = new LinkWriter<UserDto>(httpContext)
            .AddLink("self", "GET", u => $"/users/{u.Id}")
            .AddLinkForRole("Parent", "edit", "PUT", u => $"/users/{u.Id}")
            .AddLinkForRole("Parent", "delete", "DELETE", u => $"/users/{u.Id}");

        return users.Select(u =>
        {
            var dto = DtoFactory.CreateUserDto(u);
            return dto with { Links = lw.Build(dto) };
        }).ToList();
    }
}
