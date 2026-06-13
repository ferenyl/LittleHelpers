using LittleHelpers.ApiService.Application.Cqrs;

namespace LittleHelpers.ApiService.Application.Users;

[RequireRoles("Parent")]
public sealed record DeleteUserCommand(int UserId);

public sealed class DeleteUserCommandHandler(
    IUserRepository userRepository) : ICommandHandler<DeleteUserCommand, Unit>
{
    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetTrackedByIdAsync(request.UserId)
            ?? throw new RequestNotFoundException("Användaren kunde inte hittas.");

        userRepository.Remove(user);
        await userRepository.SaveChangesAsync();
        return Unit.Value;
    }
}
