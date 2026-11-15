using ImportNet.Application.UseCases.Ventas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/ventas")]
[Authorize(Policy = "Ventas")]
public sealed class VentasController(IMediator mediator)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Historial(
        DateOnly? desde,
        DateOnly? hasta,
        CancellationToken ct)
    {
        var resultado = await mediator.Send(
            new HistorialVentasQuery(desde, hasta),
            ct);

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(
        int id,
        CancellationToken ct)
    {
        var resultado = await mediator.Send(
            new GetNotaVentaQuery(id),
            ct);

        return Ok(resultado);
    }
}