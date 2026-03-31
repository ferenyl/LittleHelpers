using LittleHelpers.ApiService.Data;
using LittleHelpers.ApiService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("chorelog")]
public class ChoreLogController(AppDbContext db) : ControllerBase
{
    [HttpGet("{childId}")]
    [Authorize(Roles = "Parent,Child")]
    public async Task<IActionResult> GetForChild(int childId, [FromQuery] int? year, [FromQuery] int? month)
    {
        var now = DateTimeOffset.UtcNow;
        var filterYear = year ?? now.Year;
        var filterMonth = month ?? now.Month;

        var logs = await db.ChoreLogs
            .Where(l => l.ChildId == childId
                && l.Timestamp.Year == filterYear
                && l.Timestamp.Month == filterMonth)
            .OrderByDescending(l => l.Timestamp)
            .Join(db.Users,
                l => l.PerformedBy,
                u => u.Id,
                (l, u) => new ChoreLogDto(l.Id, l.ChoreId, l.ChoreName, l.ChildId, l.PerformedBy, u.Username, l.Points, l.Timestamp))
            .ToListAsync();

        return Ok(logs);
    }
}
