namespace LittleHelpers.ApiService.Models;

public class Chore
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsHidden { get; set; }

    /// <summary>Max antal gånger per kalenderdygn. Null = obegränsat.</summary>
    public int? MaxTimesPerDay { get; set; }

    /// <summary>Min dagar mellan genomföranden. Null = ingen begränsning. T.ex. 7 = en gång/vecka, 2 = varannan dag.</summary>
    public int? MinDaysBetween { get; set; }

    /// <summary>Max antal gånger per kalendervecka (mån-sön). Null = obegränsat.</summary>
    public int? MaxTimesPerWeek { get; set; }

    public ICollection<ChoreAssignment> ChoreAssignments { get; set; } = [];
}
