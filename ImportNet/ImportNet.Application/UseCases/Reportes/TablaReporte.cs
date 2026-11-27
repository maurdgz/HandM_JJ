using ImportNet.Domain.Entities;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record ColumnaReporte(string Titulo, string Formato = "texto");

public sealed record TablaReporte(
    string Titulo,
    string Filtros,
    string NombreArchivo,
    IReadOnlyList<ColumnaReporte> Columnas,
    IReadOnlyList<object?[]> Filas,
    object?[] Totales);

public sealed record ObtenerTablaReporteQuery(
    string Reporte,
    DateOnly? Desde,
    DateOnly? Hasta,
    string Agrupacion,
    int Top,
    DateOnly? Fecha) : IRequest<TablaReporte>;

internal sealed class ObtenerTablaReporteQueryHandler : IRequestHandler<ObtenerTablaReporteQuery, TablaReporte>
{
    private readonly IMediator mediator;

    public ObtenerTablaReporteQueryHandler(IMediator mediator)
    {
        this.mediator = mediator;
    }

    public Task<TablaReporte> Handle(ObtenerTablaReporteQuery r, CancellationToken ct) => r.Reporte switch
    {
        "ventas-por-periodo" => VentasPorPeriodo(r, ct),
        "ventas-por-vendedor" => VentasPorVendedor(r, ct),
        "top-productos" => TopProductos(r, ct),
        "margen-productos" => MargenProductos(r, ct),
        "stock-bajo" => StockBajo(ct),
        "cierre-caja" => CierreCaja(r.Fecha, ct),
        "compras-por-proveedor" => ComprasPorProveedor(r, ct),
        _ => throw new NoEncontradoException("Reporte no encontrado.")
    };

    private async Task<TablaReporte> VentasPorPeriodo(ObtenerTablaReporteQuery r, CancellationToken ct)
    {
        var agrupacion = r.Agrupacion.ToLowerInvariant() switch
        {
            "dia" => "Dia",
            "mes" => "Mes",
            _ => throw new ReglaNegocioException("La agrupación debe ser Dia o Mes.")
        };
        var (desde, hasta) = PeriodoReporte.Fechas(r.Desde, r.Hasta, agrupacion == "Mes" ? 1100 : 366);
        var datos = await mediator.Send(new VentasPorPeriodoQuery(desde, hasta, agrupacion), ct);
        var agrupadoPor = agrupacion == "Mes" ? "mes" : "día";
        return new("Ventas por periodo", $"{Rango(desde, hasta)} · Agrupación por {agrupadoPor}",
            $"{Archivo(r.Reporte, desde, hasta)}-{agrupacion.ToLowerInvariant()}",
            [new("Periodo"), new("Ventas", "entero"), new("Total", "pen")],
            datos.Select(x => new object?[] { x.Periodo, x.Ventas, x.Total }).ToArray(),
            ["Totales", datos.Sum(x => x.Ventas), datos.Sum(x => x.Total)]);
    }

    private async Task<TablaReporte> VentasPorVendedor(ObtenerTablaReporteQuery r, CancellationToken ct)
    {
        var (desde, hasta) = PeriodoReporte.Fechas(r.Desde, r.Hasta);
        var datos = await mediator.Send(new VentasPorVendedorQuery(desde, hasta), ct);
        var ventas = datos.Sum(x => x.Ventas);
        var total = datos.Sum(x => x.Total);
        return new("Ventas por vendedor", Rango(desde, hasta), Archivo(r.Reporte, desde, hasta),
            [new("Vendedor"), new("Ventas", "entero"), new("Total", "pen"), new("Ticket promedio", "pen")],
            datos.Select(x => new object?[] { x.Vendedor, x.Ventas, x.Total, x.TicketPromedio }).ToArray(),
            ["Totales", ventas, total, ventas == 0 ? 0m : total / ventas]);
    }

    private async Task<TablaReporte> TopProductos(ObtenerTablaReporteQuery r, CancellationToken ct)
    {
        var (desde, hasta) = PeriodoReporte.Fechas(r.Desde, r.Hasta);
        var datos = await mediator.Send(new TopProductosQuery(desde, hasta, r.Top), ct);
        return new("Productos más vendidos", $"{Rango(desde, hasta)} · Máximo {r.Top} productos",
            $"{Archivo(r.Reporte, desde, hasta)}-top-{r.Top}",
            [new("Código"), new("Producto"), new("Unidad"), new("Cantidad", "cantidad"), new("Total", "pen")],
            datos.Select(x => new object?[] { x.Codigo, x.Nombre, x.UnidadMedida, x.Cantidad, x.Total }).ToArray(),
            ["Totales", null, null, null, datos.Sum(x => x.Total)]);
    }

    private async Task<TablaReporte> MargenProductos(ObtenerTablaReporteQuery r, CancellationToken ct)
    {
        var (desde, hasta) = PeriodoReporte.Fechas(r.Desde, r.Hasta);
        var datos = await mediator.Send(new MargenPorProductoQuery(desde, hasta), ct);
        var ingreso = datos.Sum(x => x.Ingreso);
        var margen = datos.Sum(x => x.Margen);
        return new("Margen por producto", $"{Rango(desde, hasta)} · Costo promedio vigente",
            Archivo(r.Reporte, desde, hasta),
            [new("Código"), new("Producto"), new("Cantidad", "cantidad"), new("Ingreso", "pen"),
                new("Costo promedio", "pen4"), new("Costo", "pen"), new("Margen", "pen"), new("Margen %", "porcentaje")],
            datos.Select(x => new object?[] { x.Codigo, x.Nombre, x.Cantidad, x.Ingreso,
                x.CostoPromedio, x.Costo, x.Margen, x.MargenPorcentaje }).ToArray(),
            ["Totales", null, null, ingreso, null, datos.Sum(x => x.Costo), margen, ingreso == 0 ? null : margen * 100 / ingreso]);
    }

    private async Task<TablaReporte> StockBajo(CancellationToken ct)
    {
        var datos = await mediator.Send(new StockBajoMinimoQuery(), ct);
        return new("Stock bajo", "Inventario actual", "stock-bajo",
            [new("Código"), new("Producto"), new("Unidad"), new("Stock", "cantidad"),
                new("Mínimo", "cantidad"), new("Faltante", "cantidad")],
            datos.Select(x => new object?[] { x.Codigo, x.Nombre, x.UnidadMedida, x.Stock, x.StockMinimo, x.Faltante }).ToArray(),
            [$"Totales: {datos.Count} productos", null, null, null, null, null]);
    }

    private async Task<TablaReporte> CierreCaja(DateOnly? fecha, CancellationToken ct)
    {
        var datos = await mediator.Send(new CierreCajaQuery(fecha), ct);
        var dia = FormattableString.Invariant($"{datos.Fecha:yyyy-MM-dd}");
        return new("Cierre de caja", $"Fecha: {dia} · Hora de Perú", $"cierre-caja-{dia}",
            [new("Concepto"), new("Ventas", "entero"), new("Subtotal", "pen"),
                new("Descuento", "pen"), new("Total registrado", "pen"), new("Anuladas", "entero"), new("Monto anulado", "pen")],
            datos.PorVendedor.Select(x => new object?[] { x.Vendedor, x.Ventas, null, null, x.Total, null, null }).ToArray(),
            ["Totales", datos.Ventas, datos.Subtotal, datos.Descuento, datos.Total, datos.Anuladas, datos.MontoAnulado]);
    }

    private async Task<TablaReporte> ComprasPorProveedor(ObtenerTablaReporteQuery r, CancellationToken ct)
    {
        var (desde, hasta) = PeriodoReporte.Fechas(r.Desde, r.Hasta);
        var datos = await mediator.Send(new ComprasPorProveedorQuery(desde, hasta), ct);
        return new("Compras por proveedor", $"{Rango(desde, hasta)} · Solo compras recibidas",
            Archivo(r.Reporte, desde, hasta),
            [new("Proveedor"), new("País"), new("Compras", "entero"), new("Total USD", "usd"), new("Total PEN", "pen")],
            datos.Select(x => new object?[] { x.Proveedor, x.Pais, x.Compras, x.TotalUsd, x.TotalPen }).ToArray(),
            ["Totales", null, datos.Sum(x => x.Compras), datos.Sum(x => x.TotalUsd), datos.Sum(x => x.TotalPen)]);
    }

    private static string Rango(DateOnly desde, DateOnly hasta) =>
        FormattableString.Invariant($"{desde:yyyy-MM-dd} al {hasta:yyyy-MM-dd}");

    private static string Archivo(string reporte, DateOnly desde, DateOnly hasta) =>
        FormattableString.Invariant($"{reporte}-{desde:yyyy-MM-dd}-al-{hasta:yyyy-MM-dd}");
}
