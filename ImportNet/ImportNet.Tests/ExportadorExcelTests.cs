using ClosedXML.Excel;
using ImportNet.Application.UseCases.Reportes;
using ImportNet.Infrastructure.Reportes;
using Xunit;

namespace ImportNet.Tests;

public sealed class ExportadorExcelTests
{
    [Fact]
    public void Exportar_GeneraHojaConEncabezadosFormatoPenYTotales()
    {
        var exportador = new ExportadorExcel();
        var tabla = new TablaReporte(
            "Ventas por periodo",
            "2026-11-01 al 2026-11-27",
            "ventas-2026-11",
            [new("Periodo"), new("Ventas", "entero"), new("Total", "pen")],
            [["2026-11-01", 3, 150.50m]],
            ["Totales", 3, 150.50m]);

        var bytes = exportador.Exportar(tabla, CancellationToken.None);
        Assert.NotEmpty(bytes);

        using var stream = new MemoryStream(bytes);
        using var libro = new XLWorkbook(stream);
        var hoja = libro.Worksheet("Reporte");

        Assert.Equal("Ventas por periodo | 2026-11-01 al 2026-11-27", hoja.Cell(1, 1).GetString());
        Assert.Equal("Periodo", hoja.Cell(3, 1).GetString());
        Assert.Equal("Total", hoja.Cell(3, 3).GetString());
        Assert.Equal(150.50, hoja.Cell(4, 3).GetDouble(), 2);
        Assert.Equal("\"S/ \"#,##0.00", hoja.Cell(4, 3).Style.NumberFormat.Format);
        Assert.Equal("Totales", hoja.Cell(5, 1).GetString());
        Assert.True(hoja.Cell(5, 1).Style.Font.Bold);
    }
}
