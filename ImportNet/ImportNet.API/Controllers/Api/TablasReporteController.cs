using ImportNet.Application.UseCases.Reportes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/reportes/{reporte}")]
[Authorize(Policy = "Administracion")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TablasReporteController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IExportadorExcel _exportador;

    public TablasReporteController(IMediator mediator, IExportadorExcel exportador)
    {
        _mediator = mediator;
        _exportador = exportador;
    }

    [HttpGet("tabla")]
    public async Task<IActionResult> Tabla(string reporte, DateOnly? desde, DateOnly? hasta, DateOnly? fecha,
        string agrupacion = "Dia", int top = 10, CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new ObtenerTablaReporteQuery(reporte, desde, hasta, agrupacion, top, fecha), ct));
    }

    [HttpGet("excel")]
    public async Task<IActionResult> Excel(string reporte, DateOnly? desde, DateOnly? hasta, DateOnly? fecha,
        string agrupacion = "Dia", int top = 10, CancellationToken ct = default)
    {
        var tabla = await _mediator.Send(new ObtenerTablaReporteQuery(reporte, desde, hasta, agrupacion, top, fecha), ct);
        var archivo = _exportador.Exportar(tabla, ct);
        return File(archivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", tabla.NombreArchivo + ".xlsx");
    }
}
