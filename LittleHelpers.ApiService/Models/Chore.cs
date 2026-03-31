namespace LittleHelpers.ApiService.Models;

public class Chore
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsHidden { get; set; }

    public ICollection<ChoreAssignment> ChoreAssignments { get; set; } = [];
}
