using Microsoft.AspNetCore.Mvc;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("chorelog")]
public class ChoreLogController(
    IQueryHandler<GetChoreLogQuery, ChoreLogPeriodDto> getChoreLog,
    ICommandHandler<DeleteChoreLogCommand, Unit> deleteChoreLog) : ControllerBase
{
    [HttpGet("{childId}")]
    public async Task<IActionResult> GetForChild(int childId, [FromQuery] int? year, [FromQuery] int? month)
        => Ok(await getChoreLog.Handle(new GetChoreLogQuery(childId, year, month)));

    [HttpDelete("item/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await deleteChoreLog.Handle(new DeleteChoreLogCommand(id));
        return NoContent();
    }
}
