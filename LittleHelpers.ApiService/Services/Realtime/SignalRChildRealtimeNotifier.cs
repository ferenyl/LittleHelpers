using Microsoft.AspNetCore.SignalR;

namespace LittleHelpers.ApiService.Services.Realtime;

public sealed class SignalRChildRealtimeNotifier(
    IHubContext<ChildUpdatesHub> hubContext) : IChildRealtimeNotifier
{
    public Task NotifyChildUpdatedAsync(
        int childId,
        string changeType,
        CancellationToken cancellationToken = default)
    {
        var update = new ChildRealtimeUpdate(
            childId,
            changeType,
            DateTimeOffset.UtcNow);

        return hubContext.Clients.Group(ChildUpdatesHub.ChildGroupName(childId))
            .SendAsync(ChildUpdatesHub.ChildUpdatedEventName, update, cancellationToken);
    }
}
