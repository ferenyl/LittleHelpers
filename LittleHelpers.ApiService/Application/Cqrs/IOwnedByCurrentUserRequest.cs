namespace LittleHelpers.ApiService.Application.Cqrs;

public interface IOwnedByCurrentUserRequest
{
    int OwnerUserId { get; }
}
