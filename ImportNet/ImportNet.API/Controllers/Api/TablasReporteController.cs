using ImportNet.Application.UseCases.Reportes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/reportes/{reporte}")]
[Authorize(Policy = "Administracion")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TablasReporteController(IMediator mediator, IExportadorExcel exportador) : ControllerBase
{
    [HttpGet("tabla")]
    public Task<TablaReporte> Tabla(
        string reporte, DateOnly? desde, DateOnly? hasta, DateOnly? fecha,
        string agrupacion = "Dia", int top = 10, CancellationToken ct = default)
        => mediator.Send(new ObtenerTablaReporteQuery(reporte, desde, hasta, agrupacion, top, fecha), ct);

    [HttpGet("excel")]
    public async Task<IActionResult> Excel(
        string reporte, DateOnly? desde, DateOnly? hasta, DateOnly? fecha,
        string agrupacion = "Dia", int top = 10, CancellationToken ct = default)
    {
        var tabla = await mediator.Send(new ObtenerTablaReporteQuery(reporte, desde, hasta, agrupacion, top, fecha), ct);
        return File(exportador.Exportar(tabla, ct),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", tabla.NombreArchivo + ".xlsx");
    }
}
