using System.Security.Claims;
using LittleHelpers.ApiService.Data;
using LittleHelpers.ApiService.Models;
using LittleHelpers.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("children")]
public class ChildrenController(AppDbContext db, IHttpContextAccessor httpContext) : ControllerBase
{
    private LinkWriter<ChildSummaryDto> MakeLinkWriter() =>
        new LinkWriter<ChildSummaryDto>(httpContext)
            .AddLink("self", "GET", c => $"/children/{c.Id}");

    [HttpGet]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> GetAll()
    {
        var children = await db.Users
            .Where(u => u.UserLevel == UserLevel.Child)
            .Include(u => u.ChoreAssignments)
                .ThenInclude(ca => ca.Chore)
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        var childIds = children.Select(c => c.Id).ToList();
        var monthPoints = await db.ChoreLogs
            .Where(l => childIds.Contains(l.ChildId) && l.Timestamp.Year == now.Year && l.Timestamp.Month == now.Month)
            .GroupBy(l => l.ChildId)
            .Select(g => new { ChildId = g.Key, Points = g.Sum(l => l.Points) })
            .ToDictionaryAsync(x => x.ChildId, x => x.Points);

        var lw = MakeLinkWriter();
        var choreLw = new LinkWriter<ChoreDto>(httpContext)
            .AddLink("complete", "POST", c => $"/chores/{c.Id}/complete");

        return Ok(children.Select(c =>
        {
            var dto = ToSummaryDto(c, lw, choreLw);
            return dto with { TotalPoints = monthPoints.GetValueOrDefault(c.Id, 0) };
        }));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Parent,Child")]
    public async Task<IActionResult> GetById(int id)
    {
        var child = await db.Users
            .Where(u => u.Id == id && u.UserLevel == UserLevel.Child)
            .Include(u => u.ChoreAssignments)
                .ThenInclude(ca => ca.Chore)
            .FirstOrDefaultAsync();

        if (child is null) return NotFound();

        var lw = MakeLinkWriter();
        var choreLw = new LinkWriter<ChoreDto>(httpContext)
            .AddLink("complete", "POST", c => $"/chores/{c.Id}/complete");

        List<ChoreDto> parentChores = [];
        if (User.IsInRole("Parent"))
        {
            var parentIds = await db.Users
                .Where(u => u.UserLevel == UserLevel.Parent)
                .Select(u => u.Id)
                .ToListAsync();

            var parentChoreEntities = await db.Chores
                .Include(c => c.ChoreAssignments)
                .Where(c => !c.IsHidden && c.ChoreAssignments.Any(a => parentIds.Contains(a.UserId)))
                .ToListAsync();

            parentChores = parentChoreEntities.Select(c =>
            {
                var dto = new ChoreDto(c.Id, c.Name, c.Points, c.IsHidden,
                    c.ChoreAssignments.Select(a => a.UserId), []);
                return dto with { Links = choreLw.Build(dto) };
            }).ToList();
        }

        return Ok(ToSummaryDto(child, lw, choreLw, parentChores));
    }

    private static ChildSummaryDto ToSummaryDto(User u, LinkWriter<ChildSummaryDto> lw, LinkWriter<ChoreDto> choreLw, IEnumerable<ChoreDto>? extraChores = null)
    {
        var chores = u.ChoreAssignments
            .Where(ca => !ca.Chore.IsHidden)
            .Select(ca =>
            {
                var dto = new ChoreDto(ca.Chore.Id, ca.Chore.Name, ca.Chore.Points, ca.Chore.IsHidden,
                    ca.Chore.ChoreAssignments.Select(a => a.UserId), []);
                return dto with { Links = choreLw.Build(dto) };
            });

        var allChores = extraChores != null ? chores.Concat(extraChores) : chores;

        var summary = new ChildSummaryDto(u.Id, u.Username, 0, u.MonthlyAllowance, u.PointsGoal, allChores, []);
        return summary with { Links = lw.Build(summary) };
    }
}
