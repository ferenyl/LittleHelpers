using System.Security.Claims;
using LittleHelpers.ApiService.Data;
using LittleHelpers.ApiService.Models;
using LittleHelpers.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("users")]
[Authorize(Roles = "Parent")]
public class UsersController(AppDbContext db, IHttpContextAccessor httpContext) : ControllerBase
{
    private LinkWriter<UserDto> MakeLinkWriter() =>
        new LinkWriter<UserDto>(httpContext)
            .AddLink("self", "GET", u => $"/users/{u.Id}")
            .AddLinkForRole("Parent", "edit", "PUT", u => $"/users/{u.Id}")
            .AddLinkForRole("Parent", "delete", "DELETE", u => $"/users/{u.Id}");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await db.Users.ToListAsync();
        var lw = MakeLinkWriter();
        return Ok(users.Select(u => ToDto(u, lw)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        return Ok(ToDto(user, MakeLinkWriter()));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (!Enum.TryParse<UserLevel>(request.UserLevel, true, out var level))
            return BadRequest(Problem("Invalid user level. Use 'Parent' or 'Child'."));

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            UserLevel = level,
            MonthlyAllowance = request.MonthlyAllowance,
            PointsGoal = request.PointsGoal
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, ToDto(user, MakeLinkWriter()));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        if (request.Password is not null)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (request.UserLevel is not null)
        {
            if (!Enum.TryParse<UserLevel>(request.UserLevel, true, out var level))
                return BadRequest(Problem("Invalid user level."));
            user.UserLevel = level;
        }

        if (request.MonthlyAllowance is not null)
            user.MonthlyAllowance = request.MonthlyAllowance;
        if (request.PointsGoal is not null)
            user.PointsGoal = request.PointsGoal;

        await db.SaveChangesAsync();
        return Ok(ToDto(user, MakeLinkWriter()));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static UserDto ToDto(User u, LinkWriter<UserDto> lw)
    {
        var dto = new UserDto(u.Id, u.Username, u.UserLevel.ToString(), u.MonthlyAllowance, u.PointsGoal, []);
        return dto with { Links = lw.Build(dto) };
    }
}
