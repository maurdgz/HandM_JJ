using ImportNet.Application.UseCases.Reportes;
using ImportNet.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

/// <summary>
/// Reportes. Fechas AAAA-MM-DD en hora de Perú, ambas inclusivas; sin fechas, el mes en curso.
/// Solo cuentan ventas Registradas. Todo es solo Administrador, salvo <c>mi-resumen</c>.
/// </summary>
[ApiController]
[Route("api/reportes")]
[Authorize(Policy = "Ventas")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ReportesController(IMediator mediator) : ControllerBase
{
    /// <summary>KPI de hoy y del mes hasta hoy (Perú), netos en PEN. Solo Administrador.</summary>
    [HttpGet("dashboard")]
    [Authorize(Policy = "Administracion")]
    [ProducesResponseType(typeof(ResumenDashboard), StatusCodes.Status200OK)]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
        => Ok(await mediator.Send(new ResumenDashboardQuery(), ct));

    /// <param name="agrupacion">Dia o Mes.</param>
    [HttpGet("ventas-por-periodo")]
    [Authorize(Policy = "Administracion")]
    [ProducesResponseType(typeof(IReadOnlyList<VentasPorPeriodo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VentasPorPeriodo(
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        [FromQuery] string agrupacion = "Dia",
        CancellationToken ct = default)
        => Ok(await mediator.Send(new VentasPorPeriodoQuery(desde, hasta, agrupacion), ct));

    [HttpGet("ventas-por-vendedor")]
    [Authorize(Policy = "Administracion")]
    [ProducesResponseType(typeof(IReadOnlyList<VentasPorVendedor>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VentasPorVendedor(
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new VentasPorVendedorQuery(desde, hasta), ct));

    /// <param name="top">Entre 1 y 50; por defecto 10.</param>
    [HttpGet("top-productos")]
    [Authorize(Policy = "Administracion")]
    [ProducesResponseType(typeof(IReadOnlyList<TopProducto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TopProductos(
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        [FromQuery] int top = 10,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new TopProductosQuery(desde, hasta, top), ct));

    [HttpGet("margen-productos")]
    [Authorize(Policy = "Administracion")]
    [ProducesResponseType(typeof(IReadOnlyList<MargenProducto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MargenProductos(
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new MargenPorProductoQuery(desde, hasta), ct));

    [HttpGet("stock-bajo")]
    [Authorize(Policy = "Administracion")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductoStockBajo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StockBajo(CancellationToken ct)
        => Ok(await mediator.Send(new StockBajoMinimoQuery(), ct));

    /// <param name="fecha">Día a cerrar; por defecto hoy.</param>
    [HttpGet("cierre-caja")]
    [Authorize(Policy = "Administracion")]
    [ProducesResponseType(typeof(CierreCaja), StatusCodes.Status200OK)]
    public async Task<IActionResult> CierreCaja(
        [FromQuery] DateOnly? fecha = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new CierreCajaQuery(fecha), ct));

    /// <summary>Resumen diario de las ventas propias. Administrador y Vendedor.</summary>
    /// <param name="fecha">Por defecto hoy.</param>
    [HttpGet("mi-resumen")]
    [ProducesResponseType(typeof(CierreCaja), StatusCodes.Status200OK)]
    public async Task<IActionResult> MiResumen(
        [FromQuery] DateOnly? fecha = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new MiResumenDiarioQuery(fecha), ct));

    [HttpGet("compras-por-proveedor")]
    [Authorize(Policy = "Administracion")]
    [ProducesResponseType(typeof(IReadOnlyList<ComprasPorProveedor>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ComprasPorProveedor(
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new ComprasPorProveedorQuery(desde, hasta), ct));
}
