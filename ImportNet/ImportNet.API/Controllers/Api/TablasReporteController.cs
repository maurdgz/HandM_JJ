using ImportNet.Application.UseCases.Reportes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/reportes/{reporte}")]
[Authorize(Policy = "Administracion")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TablasReporteController(IMediator mediator) : ControllerBase
{
    [HttpGet("tabla")]
    [ProducesResponseType(typeof(TablaReporte), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Tabla(
        string reporte,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly? fecha,
        string agrupacion = "Dia",
        int top = 10,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new ObtenerTablaReporteQuery(reporte, desde, hasta, agrupacion, top, fecha), ct));

    [HttpGet("excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excel(
        string reporte,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly? fecha,
        string agrupacion = "Dia",
        int top = 10,
        CancellationToken ct = default)
    {
        var archivo = await mediator.Send(
            new ExportarTablaReporteQuery(reporte, desde, hasta, agrupacion, top, fecha),
            ct);

        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }
}
