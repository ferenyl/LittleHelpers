using System.Security.Claims;
using LittleHelpers.ApiService.Models;
using Microsoft.AspNetCore.Mvc;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("menu")]
public class MenuController : ControllerBase
{
    [HttpGet]
    public IActionResult GetMenu()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var items = new List<MenuItemDto>();

        if (role == "Parent")
        {
            items.Add(new MenuItemDto("menu.chores", "/chores"));
            items.Add(new MenuItemDto("menu.children", "/children"));
            items.Add(new MenuItemDto("menu.users", "/users"));
        }

        if (role == "Child" && userId is not null)
        {
            items.Add(new MenuItemDto("menu.myChores", $"/children/{userId}"));
        }

        return Ok(items);
    }
}
