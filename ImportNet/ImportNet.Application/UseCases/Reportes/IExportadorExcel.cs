namespace ImportNet.Application.UseCases.Reportes;

public interface IExportadorExcel
{
    byte[] Exportar(TablaReporte tabla, CancellationToken ct);
}
