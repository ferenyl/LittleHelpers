using Microsoft.AspNetCore.Mvc;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("children")]
public class ChildrenController(
    IQueryHandler<GetChildrenQuery, IReadOnlyList<ChildSummaryDto>> getChildren,
    IQueryHandler<GetChildDetailQuery, ChildSummaryDto> getChildDetail) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await getChildren.Handle(new GetChildrenQuery()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
        => Ok(await getChildDetail.Handle(new GetChildDetailQuery(id)));
}
