using ImportNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed partial class ReporteRepository
{
    private IQueryable<LineaProductoReporte> LineasProductosPostgres(DateTime desdeUtc, DateTime hastaUtc)
        => from venta in VentasRegistradas(desdeUtc, hastaUtc)
           join detalle in db.Set<DetalleVenta>().AsNoTracking() on venta.Id equals detalle.VentaId
           join producto in db.Set<Producto>().AsNoTracking() on detalle.ProductoId equals producto.Id
           select new LineaProductoReporte { ProductoId = producto.Id, Codigo = producto.Codigo,
               Nombre = producto.Nombre, UnidadMedida = producto.UnidadMedida, CostoPromedio = producto.CostoPromedio,
               Cantidad = detalle.Cantidad, Subtotal = detalle.Subtotal };

    private async Task<IReadOnlyList<TopProducto>> TopProductosPostgres(
        DateTime desdeUtc, DateTime hastaUtc, int top, CancellationToken ct)
        => await LineasProductosPostgres(desdeUtc, hastaUtc)
            .GroupBy(x => new { x.ProductoId, x.Codigo, x.Nombre, x.UnidadMedida })
            .Select(g => new TopProducto(g.Key.ProductoId, g.Key.Codigo, g.Key.Nombre, g.Key.UnidadMedida,
                g.Sum(x => x.Cantidad), g.Sum(x => x.Subtotal)))
            .OrderByDescending(x => x.Cantidad).ThenByDescending(x => x.Total).ThenBy(x => x.ProductoId)
            .Take(top).ToListAsync(ct);

    private async Task<IReadOnlyList<MargenProducto>> MargenPorProductoPostgres(
        DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct)
    {
        var filas = await LineasProductosPostgres(desdeUtc, hastaUtc)
            .GroupBy(x => new { x.ProductoId, x.Codigo, x.Nombre, x.CostoPromedio })
            .Select(g => new { g.Key.ProductoId, g.Key.Codigo, g.Key.Nombre, g.Key.CostoPromedio,
                Cantidad = g.Sum(x => x.Cantidad), Ingreso = g.Sum(x => x.Subtotal) }).ToListAsync(ct);
        return filas.Select(x => {
            var costo = decimal.Round(x.Cantidad * x.CostoPromedio, 2, MidpointRounding.AwayFromZero);
            var margen = x.Ingreso - costo;
            return new MargenProducto(x.ProductoId, x.Codigo, x.Nombre, x.Cantidad, x.Ingreso,
                x.CostoPromedio, costo, margen, x.Ingreso == 0 ? 0 : decimal.Round(margen / x.Ingreso * 100, 2, MidpointRounding.AwayFromZero));
        }).OrderByDescending(x => x.Margen).ThenBy(x => x.ProductoId).ToArray();
    }

    private sealed class LineaProductoReporte
    {
        public int ProductoId { get; init; }
        public string Codigo { get; init; } = "";
        public string Nombre { get; init; } = "";
        public string UnidadMedida { get; init; } = "";
        public decimal CostoPromedio { get; init; }
        public decimal Cantidad { get; init; }
        public decimal Subtotal { get; init; }
    }

    private async Task<IReadOnlyList<VentasPorPeriodo>> VentasPorPeriodoPostgres(
        DateTime desdeUtc, DateTime hastaUtc, AgrupacionPeriodo agrupacion, CancellationToken ct)
    {
        // AddHours se traduce antes de extraer año/mes/día: no depende del TimeZone del servidor.
        var fechas = VentasRegistradas(desdeUtc, hastaUtc)
            .Select(x => new { Local = x.Fecha.AddHours(-5), x.Total });
        var filas = await fechas.GroupBy(x => new {
            x.Local.Year, x.Local.Month, Dia = agrupacion == AgrupacionPeriodo.Mes ? 1 : x.Local.Day
        }).Select(g => new { g.Key.Year, g.Key.Month, g.Key.Dia, Ventas = g.Count(), Total = g.Sum(x => x.Total) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Dia).ToListAsync(ct);
        return filas.Select(x => new VentasPorPeriodo(
            agrupacion == AgrupacionPeriodo.Mes ? $"{x.Year:D4}-{x.Month:D2}" : $"{x.Year:D4}-{x.Month:D2}-{x.Dia:D2}",
            x.Ventas, x.Total)).ToArray();
    }

    private async Task<IReadOnlyList<VentasPorVendedor>> VentasPorVendedorPostgres(
        DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct)
    {
        var filas = await (from venta in VentasRegistradas(desdeUtc, hastaUtc)
                           join empleado in db.Set<Empleado>().AsNoTracking() on venta.EmpleadoId equals empleado.Id
                           select new { empleado.Id, empleado.Nombre, empleado.Apellidos, venta.Total })
            .GroupBy(x => new { x.Id, x.Nombre, x.Apellidos })
            .Select(g => new { g.Key.Id, Nombre = g.Key.Nombre + " " + g.Key.Apellidos,
                Ventas = g.Count(), Total = g.Sum(x => x.Total) })
            .OrderByDescending(x => x.Total).ThenBy(x => x.Nombre).ToListAsync(ct);
        return filas.Select(x => new VentasPorVendedor(x.Id, x.Nombre.Trim(), x.Ventas, x.Total,
            decimal.Round(x.Total / x.Ventas, 2, MidpointRounding.AwayFromZero))).ToArray();
    }
}
