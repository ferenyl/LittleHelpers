using System.Security.Claims;
using LittleHelpers.ApiService.Data;
using LittleHelpers.ApiService.Models;
using LittleHelpers.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("chores")]
public class ChoresController(AppDbContext db, IHttpContextAccessor httpContext) : ControllerBase
{
    private LinkWriter<ChoreDto> MakeLinkWriter() =>
        new LinkWriter<ChoreDto>(httpContext)
            .AddLink("self", "GET", c => $"/chores/{c.Id}")
            .AddLinkForRole("Parent", "edit", "PUT", c => $"/chores/{c.Id}")
            .AddLinkForRole("Parent", "delete", "DELETE", c => $"/chores/{c.Id}")
            .AddLink("complete", "POST", c => $"/chores/{c.Id}/complete");

    [HttpGet]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> GetAll()
    {
        var chores = await db.Chores
            .Include(c => c.ChoreAssignments)
            .ToListAsync();
        var lw = MakeLinkWriter();
        return Ok(chores.Select(c => ToDto(c, lw)));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> GetById(int id)
    {
        var chore = await db.Chores
            .Include(c => c.ChoreAssignments)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (chore is null) return NotFound();
        return Ok(ToDto(chore, MakeLinkWriter()));
    }

    [HttpPost]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> Create([FromBody] CreateChoreRequest request)
    {
        var chore = new Chore { Name = request.Name, Points = request.Points };
        db.Chores.Add(chore);
        await db.SaveChangesAsync();

        foreach (var userId in request.AssignedUserIds)
            db.ChoreAssignments.Add(new ChoreAssignment { ChoreId = chore.Id, UserId = userId });
        await db.SaveChangesAsync();

        await db.Entry(chore).Collection(c => c.ChoreAssignments).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = chore.Id }, ToDto(chore, MakeLinkWriter()));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateChoreRequest request)
    {
        var chore = await db.Chores
            .Include(c => c.ChoreAssignments)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (chore is null) return NotFound();

        if (request.Name is not null) chore.Name = request.Name;
        if (request.Points is not null) chore.Points = request.Points.Value;
        if (request.IsHidden is not null) chore.IsHidden = request.IsHidden.Value;

        if (request.AssignedUserIds is not null)
        {
            db.ChoreAssignments.RemoveRange(chore.ChoreAssignments);
            foreach (var userId in request.AssignedUserIds)
                db.ChoreAssignments.Add(new ChoreAssignment { ChoreId = chore.Id, UserId = userId });
        }

        await db.SaveChangesAsync();
        return Ok(ToDto(chore, MakeLinkWriter()));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> Delete(int id)
    {
        var chore = await db.Chores.FindAsync(id);
        if (chore is null) return NotFound();
        db.Chores.Remove(chore);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Parent,Child")]
    public async Task<IActionResult> Complete(int id, [FromQuery] int? targetChildId = null)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var chore = await db.Chores
            .Include(c => c.ChoreAssignments)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (chore is null) return NotFound();

        int childId;
        if (userRole == "Child")
        {
            var isAssigned = chore.ChoreAssignments.Any(a => a.UserId == userId);
            if (!isAssigned) return Forbid();
            childId = userId;
        }
        else
        {
            // If a targetChildId is provided (parent viewing child's page), use it
            if (targetChildId.HasValue)
            {
                childId = targetChildId.Value;
            }
            else
            {
                var firstChild = chore.ChoreAssignments.FirstOrDefault();
                childId = firstChild?.UserId ?? userId;
            }
        }

        var log = new ChoreLog
        {
            ChoreId = chore.Id,
            ChoreName = chore.Name,
            ChildId = childId,
            PerformedBy = userId,
            Points = chore.Points,
            Timestamp = DateTimeOffset.UtcNow
        };

        db.ChoreLogs.Add(log);
        await db.SaveChangesAsync();

        var performer = await db.Users.FindAsync(userId);
        return Ok(new ChoreLogDto(log.Id, log.ChoreId, log.ChoreName, log.ChildId, log.PerformedBy, performer?.Username ?? "", log.Points, log.Timestamp));
    }

    private static ChoreDto ToDto(Chore c, LinkWriter<ChoreDto> lw)
    {
        var dto = new ChoreDto(c.Id, c.Name, c.Points, c.IsHidden,
            c.ChoreAssignments.Select(a => a.UserId), []);
        return dto with { Links = lw.Build(dto) };
    }
}
