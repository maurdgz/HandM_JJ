using ImportNet.Application.UseCases.Reportes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/reportes")]
[Authorize(Policy = "Ventas")]
public class ReportesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = "Administracion")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ResumenDashboardQuery(), ct));
    }

    [HttpGet("ventas-por-periodo")]
    [Authorize(Policy = "Administracion")]
    public async Task<IActionResult> VentasPorPeriodo(DateOnly? desde = null, DateOnly? hasta = null, string agrupacion = "Dia",
        CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new VentasPorPeriodoQuery(desde, hasta, agrupacion), ct));
    }

    [HttpGet("ventas-por-vendedor")]
    [Authorize(Policy = "Administracion")]
    public async Task<IActionResult> VentasPorVendedor(DateOnly? desde = null, DateOnly? hasta = null, CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new VentasPorVendedorQuery(desde, hasta), ct));
    }

    [HttpGet("top-productos")]
    [Authorize(Policy = "Administracion")]
    public async Task<IActionResult> TopProductos(DateOnly? desde = null, DateOnly? hasta = null, int top = 10, CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new TopProductosQuery(desde, hasta, top), ct));
    }

    [HttpGet("margen-productos")]
    [Authorize(Policy = "Administracion")]
    public async Task<IActionResult> MargenProductos(DateOnly? desde = null, DateOnly? hasta = null, CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new MargenPorProductoQuery(desde, hasta), ct));
    }

    [HttpGet("stock-bajo")]
    [Authorize(Policy = "Administracion")]
    public async Task<IActionResult> StockBajo(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new StockBajoMinimoQuery(), ct));
    }

    [HttpGet("cierre-caja")]
    [Authorize(Policy = "Administracion")]
    public async Task<IActionResult> CierreCaja(DateOnly? fecha = null, CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new CierreCajaQuery(fecha), ct));
    }

    [HttpGet("mi-resumen")]
    public async Task<IActionResult> MiResumen(DateOnly? fecha = null, CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new MiResumenDiarioQuery(fecha), ct));
    }

    [HttpGet("compras-por-proveedor")]
    [Authorize(Policy = "Administracion")]
    public async Task<IActionResult> ComprasPorProveedor(DateOnly? desde = null, DateOnly? hasta = null, CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new ComprasPorProveedorQuery(desde, hasta), ct));
    }
}
