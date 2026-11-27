using ImportNet.API.Contracts;
using ImportNet.Application.UseCases.Ventas;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/ventas")]
[Authorize(Policy = "Ventas")]
public sealed class VentasController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(VentaRegistradaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(RegistrarVentaCommand command, CancellationToken ct)
    {
        var venta = await mediator.Send(command, ct);
        return Created($"/api/ventas/{venta.Id}", venta);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Pagina<ResumenVenta>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Historial(
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new ConsultarVentasQuery(desde, hasta, pagina, tamano), ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(NotaVenta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Nota(int id, CancellationToken ct)
        => Ok(await mediator.Send(new ObtenerNotaVentaQuery(id), ct));

    [HttpPost("{id:int}/anulacion"), Authorize(Policy = "Administracion")]
    [ProducesResponseType(typeof(VentaAnuladaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Anular(int id, AnularVentaRequest body, CancellationToken ct)
        => Ok(await mediator.Send(new AnularVentaCommand(id, body.Motivo), ct));
}
