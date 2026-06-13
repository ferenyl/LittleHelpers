using Microsoft.AspNetCore.Mvc;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("chorelog")]
public class ChoreLogController(IQueryHandler<GetChoreLogQuery, IReadOnlyList<ChoreLogDto>> getChoreLog) : ControllerBase
{
    [HttpGet("{childId}")]
    public async Task<IActionResult> GetForChild(int childId, [FromQuery] int? year, [FromQuery] int? month)
        => Ok(await getChoreLog.Handle(new GetChoreLogQuery(childId, year, month)));
}
