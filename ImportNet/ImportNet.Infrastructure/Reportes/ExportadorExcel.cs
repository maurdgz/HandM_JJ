using ClosedXML.Excel;
using ImportNet.Application.UseCases.Reportes;

namespace ImportNet.Infrastructure.Reportes;

internal sealed class ExportadorExcel : IExportadorExcel
{
    private const int FilaEncabezados = 3;

    public byte[] Exportar(TablaReporte tabla, CancellationToken ct)
    {
        using var libro = new XLWorkbook();
        libro.Properties.Title = $"{tabla.Titulo} | {tabla.Filtros}";
        var hoja = libro.Worksheets.Add("Reporte");
        var ultimaColumna = tabla.Columnas.Count;

        hoja.Range(1, 1, 1, ultimaColumna).Merge();
        hoja.Cell(1, 1).Value = libro.Properties.Title;
        hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        hoja.Cell(1, 1).Style.Alignment.WrapText = true;
        hoja.Row(1).Height = 32;

        for (var i = 0; i < ultimaColumna; i++)
        {
            var columna = tabla.Columnas[i];
            hoja.Cell(FilaEncabezados, i + 1).Value = columna.Titulo;
            hoja.Column(i + 1).Width = columna.Formato == "texto" ? 32 : Math.Max(18, columna.Titulo.Length + 3);
        }

        var encabezados = hoja.Range(FilaEncabezados, 1, FilaEncabezados, ultimaColumna);
        encabezados.Style.Font.Bold = true;
        encabezados.Style.Font.FontColor = XLColor.White;
        encabezados.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
        hoja.SheetView.FreezeRows(FilaEncabezados);

        var fila = FilaEncabezados + 1;
        foreach (var valores in tabla.Filas)
        {
            ct.ThrowIfCancellationRequested();
            EscribirFila(hoja, fila++, tabla.Columnas, valores);
        }

        EscribirFila(hoja, fila, tabla.Columnas, tabla.Totales);
        var totales = hoja.Range(fila, 1, fila, ultimaColumna);
        totales.Style.Font.Bold = true;
        totales.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        ct.ThrowIfCancellationRequested();

        using var archivo = new MemoryStream();
        libro.SaveAs(archivo);
        return archivo.ToArray();
    }

    private static void EscribirFila(IXLWorksheet hoja, int fila,
        IReadOnlyList<ColumnaReporte> columnas, IReadOnlyList<object?> valores)
    {
        for (var i = 0; i < columnas.Count; i++)
        {
            var celda = hoja.Cell(fila, i + 1);
            switch (valores[i])
            {
                case string texto:
                    celda.Value = texto;
                    celda.Style.Alignment.WrapText = true;
                    break;
                case int entero:
                    celda.Value = entero;
                    break;
                case decimal numero:
                    celda.Value = numero;
                    break;
                case null:
                    continue;
                default:
                    throw new InvalidOperationException("Tipo de celda de reporte no admitido.");
            }

            var formato = columnas[i].Formato switch
            {
                "pen" => "\"S/ \"#,##0.00",
                "pen4" => "\"S/ \"#,##0.0000",
                "usd" => "\"USD \"#,##0.00",
                "cantidad" => "#,##0.###",
                "porcentaje" => "0.00\"%\"",
                _ => null
            };
            if (formato is not null)
                celda.Style.NumberFormat.Format = formato;
        }
    }
}
