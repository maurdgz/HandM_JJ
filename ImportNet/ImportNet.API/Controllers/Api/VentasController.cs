using ImportNet.Application.UseCases.Ventas;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController, Route("api/ventas"), Authorize(Policy = "Ventas")]
public sealed class VentasController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Registrar(RegistrarVentaCommand command, CancellationToken ct)
    {
        var venta = await mediator.Send(command, ct);
        return Created($"/api/ventas/{venta.Id}", venta);
    }

    /// <summary>Historial por fecha (hora de Perú). El Vendedor solo recibe sus ventas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(Pagina<ResumenVenta>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Historial(
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20,
        CancellationToken ct = default)
    {
        var resultado = await mediator.Send(new ConsultarVentasQuery(desde, hasta, pagina, tamano), ct);
        return Ok(resultado);
    }

    /// <summary>Nota de venta. El Vendedor solo puede ver las suyas.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(NotaVenta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Nota(int id, CancellationToken ct)
        => Ok(await mediator.Send(new ObtenerNotaVentaQuery(id), ct));

    /// <summary>Anula una venta Registrada y repone su stock una sola vez. Solo Administrador.</summary>
    [HttpPost("{id:int}/anulacion")]
    [Authorize(Policy = "Administracion")]
    [ProducesResponseType(typeof(VentaAnuladaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Anular(int id, AnularVentaBody body, CancellationToken ct)
        => Ok(await mediator.Send(new AnularVentaCommand(id, body.Motivo), ct));
}

public sealed record AnularVentaBody(string Motivo);
