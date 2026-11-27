using ImportNet.API.Contracts;
using ImportNet.Application.UseCases.AsistenteIa;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/ia")]
[Authorize(Policy = "Administracion")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AsistenteIaController(IMediator mediator) : ControllerBase
{
    /// <summary>Analiza KPIs, márgenes y alertas de stock usando el servicio externo de IA configurado.</summary>
    [HttpPost("analizar")]
    [ProducesResponseType(typeof(AnalisisIaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Analizar([FromBody] AnalizarNegocioIaRequest? request, CancellationToken ct)
        => Ok(await mediator.Send(new AnalizarNegocioIaCommand(request?.Desde, request?.Hasta, request?.Pregunta), ct));
}
