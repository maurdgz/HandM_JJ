using ImportNet.Application.UseCases.Reportes;
using ImportNet.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

/// <summary>
/// Reportes. Fechas AAAA-MM-DD en hora de Perú, ambas inclusivas; sin fechas, el mes en curso.
/// Solo cuentan ventas Registradas.
/// </summary>
[ApiController]
[Route("api/reportes")]
[Authorize(Policy = "Administracion")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ReportesController(IMediator mediator) : ControllerBase
{
    /// <param name="agrupacion">Dia o Mes.</param>
    [HttpGet("ventas-por-periodo")]
    [ProducesResponseType(typeof(IReadOnlyList<VentasPorPeriodo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VentasPorPeriodo(
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        [FromQuery] string agrupacion = "Dia",
        CancellationToken ct = default)
        => Ok(await mediator.Send(new VentasPorPeriodoQuery(desde, hasta, agrupacion), ct));

    [HttpGet("ventas-por-vendedor")]
    [ProducesResponseType(typeof(IReadOnlyList<VentasPorVendedor>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VentasPorVendedor(
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new VentasPorVendedorQuery(desde, hasta), ct));

    /// <param name="top">Entre 1 y 50; por defecto 10.</param>
    [HttpGet("top-productos")]
    [ProducesResponseType(typeof(IReadOnlyList<TopProducto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TopProductos(
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        [FromQuery] int top = 10,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new TopProductosQuery(desde, hasta, top), ct));

    [HttpGet("margen-productos")]
    [ProducesResponseType(typeof(IReadOnlyList<MargenProducto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MargenProductos(
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new MargenPorProductoQuery(desde, hasta), ct));
}
