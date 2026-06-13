using Microsoft.AspNetCore.Mvc;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("menu")]
public class MenuController(IQueryHandler<GetMenuQuery, IReadOnlyList<MenuItemDto>> getMenu) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMenu()
        => Ok(await getMenu.Handle(new GetMenuQuery()));
}
