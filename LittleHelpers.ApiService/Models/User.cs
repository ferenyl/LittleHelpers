namespace LittleHelpers.ApiService.Models;

public enum UserLevel { Parent, Child }

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserLevel UserLevel { get; set; }

    /// <summary>Månadsersättning i kronor (null = ej satt).</summary>
    public decimal? MonthlyAllowance { get; set; }

    /// <summary>Antal poäng som krävs för full månadsersättning (null = ej satt).</summary>
    public int? PointsGoal { get; set; }

    public ICollection<ChoreAssignment> ChoreAssignments { get; set; } = [];
}
