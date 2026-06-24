using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LittleHelpers.ApiService.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    IQueryHandler<LoginQuery, LoginResponse> login,
    IQueryHandler<RenewTokenQuery, RenewTokenResponse> renewToken) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
        => Ok(await login.Handle(new LoginQuery(request.Username, request.Password)));

    [HttpPost("renew")]
    public async Task<IActionResult> Renew()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            throw new RequestAuthenticationException("Missing user identifier claim.");

        return Ok(await renewToken.Handle(new RenewTokenQuery(userId)));
    }
}
