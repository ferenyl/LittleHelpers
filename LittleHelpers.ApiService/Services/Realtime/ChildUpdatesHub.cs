using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LittleHelpers.ApiService.Services.Realtime;

[Authorize]
public sealed class ChildUpdatesHub : Hub
{
    public const string ChildUpdatedEventName = "childUpdated";

    public static string ChildGroupName(int childId) => $"child:{childId}";

    public Task JoinChildGroup(int childId)
        => Groups.AddToGroupAsync(Context.ConnectionId, ChildGroupName(childId));

    public Task LeaveChildGroup(int childId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, ChildGroupName(childId));
}
