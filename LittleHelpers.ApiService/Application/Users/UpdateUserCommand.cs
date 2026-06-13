using LittleHelpers.ApiService.Application.Cqrs;

namespace LittleHelpers.ApiService.Application.Users;

[RequireRoles("Parent")]
public sealed record UpdateUserCommand(
    int UserId,
    string? Password,
    string? UserLevel,
    decimal? MonthlyAllowance = null,
    int? PointsGoal = null);

public sealed class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IHttpContextAccessor httpContext) : ICommandHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetTrackedByIdAsync(request.UserId)
            ?? throw new RequestNotFoundException("Användaren kunde inte hittas.");

        if (request.Password is not null)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (request.UserLevel is not null)
        {
            if (!Enum.TryParse<UserLevel>(request.UserLevel, true, out var level))
                throw new RequestValidationException("Invalid user level.");
            user.UserLevel = level;
        }

        if (request.MonthlyAllowance is not null)
            user.MonthlyAllowance = request.MonthlyAllowance;
        if (request.PointsGoal is not null)
            user.PointsGoal = request.PointsGoal;

        await userRepository.SaveChangesAsync();

        var lw = new LinkWriter<UserDto>(httpContext)
            .AddLink("self", "GET", u => $"/users/{u.Id}")
            .AddLinkForRole("Parent", "edit", "PUT", u => $"/users/{u.Id}")
            .AddLinkForRole("Parent", "delete", "DELETE", u => $"/users/{u.Id}");

        var dto = DtoFactory.CreateUserDto(user);
        return dto with { Links = lw.Build(dto) };
    }
}
