namespace LittleHelpers.ApiService.Services.Realtime;

public sealed record ChildRealtimeUpdate(
    int ChildId,
    string ChangeType,
    DateTimeOffset ChangedAtUtc);
