using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IQueryHandler<LoginQuery, LoginResponse> login) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
        => Ok(await login.Handle(new LoginQuery(request.Username, request.Password)));
}
