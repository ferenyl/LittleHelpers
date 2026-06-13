using System.Security.Claims;
using LittleHelpers.ApiService.Application.Cqrs;

namespace LittleHelpers.ApiService.Application.Menu;

public sealed record GetMenuQuery;

public sealed class GetMenuQueryHandler(IHttpContextAccessor httpContext) : IQueryHandler<GetMenuQuery, IReadOnlyList<MenuItemDto>>
{
    public Task<IReadOnlyList<MenuItemDto>> Handle(GetMenuQuery request, CancellationToken cancellationToken = default)
    {
        var role = httpContext.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
        var userId = httpContext.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        var items = new List<MenuItemDto>();

        if (role == "Parent")
        {
            items.Add(new MenuItemDto("menu.chores", "/chores"));
            items.Add(new MenuItemDto("menu.children", "/children"));
            items.Add(new MenuItemDto("menu.users", "/users"));
        }

        if (role == "Child" && userId is not null)
            items.Add(new MenuItemDto("menu.myChores", $"/children/{userId}"));

        return Task.FromResult<IReadOnlyList<MenuItemDto>>(items);
    }
}
