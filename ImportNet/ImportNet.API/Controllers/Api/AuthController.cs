using ImportNet.API.Configuration;
using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous]
    [EnableRateLimiting(LoginRateLimiting.Policy)]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    [HttpGet("me"), Authorize]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(await mediator.Send(new GetUsuarioActualQuery(), ct));
}
