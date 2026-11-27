using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record ArchivoExcelDto(byte[] Contenido, string NombreArchivo, string ContentType);

public sealed record ExportarTablaReporteQuery(
    string Reporte,
    DateOnly? Desde,
    DateOnly? Hasta,
    string Agrupacion,
    int Top,
    DateOnly? Fecha) : IRequest<ArchivoExcelDto>;

internal sealed class ExportarTablaReporteQueryHandler(IMediator mediator, IExportadorExcel exportador)
    : IRequestHandler<ExportarTablaReporteQuery, ArchivoExcelDto>
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<ArchivoExcelDto> Handle(ExportarTablaReporteQuery request, CancellationToken cancellationToken)
    {
        var tabla = await mediator.Send(
            new ObtenerTablaReporteQuery(
                request.Reporte,
                request.Desde,
                request.Hasta,
                request.Agrupacion,
                request.Top,
                request.Fecha),
            cancellationToken);

        var contenido = exportador.Exportar(tabla, cancellationToken);
        return new ArchivoExcelDto(contenido, $"{tabla.NombreArchivo}.xlsx", ExcelContentType);
    }
}
