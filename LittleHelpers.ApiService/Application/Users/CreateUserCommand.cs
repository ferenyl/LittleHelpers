using LittleHelpers.ApiService.Application.Cqrs;

namespace LittleHelpers.ApiService.Application.Users;

[RequireRoles("Parent")]
public sealed record CreateUserCommand(
    string Username,
    string Password,
    string UserLevel,
    decimal? MonthlyAllowance = null,
    int? PointsGoal = null);

public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IHttpContextAccessor httpContext) : ICommandHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserLevel>(request.UserLevel, true, out var level))
            throw new RequestValidationException("Invalid user level. Use 'Parent' or 'Child'.");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            UserLevel = level,
            MonthlyAllowance = request.MonthlyAllowance,
            PointsGoal = request.PointsGoal
        };

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        var lw = new LinkWriter<UserDto>(httpContext)
            .AddLink("self", "GET", u => $"/users/{u.Id}")
            .AddLinkForRole("Parent", "edit", "PUT", u => $"/users/{u.Id}")
            .AddLinkForRole("Parent", "delete", "DELETE", u => $"/users/{u.Id}");

        var dto = DtoFactory.CreateUserDto(user);
        return dto with { Links = lw.Build(dto) };
    }
}
