namespace LittleHelpers.ApiService.Services;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateTime UtcNowDateTime { get; }
}

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTime UtcNowDateTime => DateTime.UtcNow;
}
