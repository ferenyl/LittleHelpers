namespace LittleHelpers.ApiService.Models;

public class ChoreAssignment
{
    public int ChoreId { get; set; }
    public Chore Chore { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
