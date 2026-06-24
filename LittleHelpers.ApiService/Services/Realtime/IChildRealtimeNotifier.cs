namespace LittleHelpers.ApiService.Services.Realtime;

public interface IChildRealtimeNotifier
{
    Task NotifyChildUpdatedAsync(
        int childId,
        string changeType,
        CancellationToken cancellationToken = default);
}
