using Microsoft.AspNetCore.Mvc;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("chores")]
public class ChoresController(
    IQueryHandler<GetChoresQuery, IReadOnlyList<ChoreDto>> getChores,
    IQueryHandler<GetChoreByIdQuery, ChoreDto> getChoreById,
    ICommandHandler<CreateChoreCommand, ChoreDto> createChore,
    ICommandHandler<UpdateChoreCommand, ChoreDto> updateChore,
    ICommandHandler<DeleteChoreCommand, Unit> deleteChore,
    ICommandHandler<CompleteChoreCommand, ChoreLogDto> completeChore) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await getChores.Handle(new GetChoresQuery()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
        => Ok(await getChoreById.Handle(new GetChoreByIdQuery(id)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChoreCommand request)
    {
        var created = await createChore.Handle(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateChoreCommand request)
        => Ok(await updateChore.Handle(request with { ChoreId = id }));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await deleteChore.Handle(new DeleteChoreCommand(id));
        return NoContent();
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id, [FromQuery] int? targetChildId = null)
        => Ok(await completeChore.Handle(new CompleteChoreCommand(id, targetChildId)));
}
