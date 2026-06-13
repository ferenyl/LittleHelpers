using Microsoft.AspNetCore.Mvc;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("users")]
public class UsersController(
    IQueryHandler<GetUsersQuery, IReadOnlyList<UserDto>> getUsers,
    IQueryHandler<GetUserByIdQuery, UserDto> getUserById,
    ICommandHandler<CreateUserCommand, UserDto> createUser,
    ICommandHandler<UpdateUserCommand, UserDto> updateUser,
    ICommandHandler<DeleteUserCommand, Unit> deleteUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await getUsers.Handle(new GetUsersQuery()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
        => Ok(await getUserById.Handle(new GetUserByIdQuery(id)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand request)
    {
        var created = await createUser.Handle(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserCommand request)
        => Ok(await updateUser.Handle(request with { UserId = id }));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await deleteUser.Handle(new DeleteUserCommand(id));
        return NoContent();
    }
}
