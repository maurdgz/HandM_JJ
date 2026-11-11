using ImportNet.Application.UseCases.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController, Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous]
    [ProducesResponseType(200), ProducesResponseType(401)]
    public async Task<IActionResult> Login(LoginQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    [HttpGet("me"), Authorize]
    [ProducesResponseType(200), ProducesResponseType(401)]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(await mediator.Send(new GetUsuarioActualQuery(), ct));
}
