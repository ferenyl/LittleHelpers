namespace LittleHelpers.ApiService.Models;

public class ChoreLog
{
    public int Id { get; set; }
    public int ChoreId { get; set; }
    public string ChoreName { get; set; } = string.Empty;
    public int ChildId { get; set; }
    public int PerformedBy { get; set; }
    public int Points { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
