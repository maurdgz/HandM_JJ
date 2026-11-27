using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class DashboardRepository(AppDbContext db) : IDashboardRepository
{
    public async Task<ResumenDashboard> Resumen(DateOnly fecha, DateTime inicioDiaUtc,
        DateTime finDiaUtc, DateTime inicioMesUtc, CancellationToken ct)
    {
        // Los tres agregados deben observar la misma versión aunque se registre una venta al consultarlos.
        await using var snapshot = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(db.Database.IsNpgsql()
                ? System.Data.IsolationLevel.RepeatableRead : System.Data.IsolationLevel.Serializable, ct)
            : null;
        var ventas = db.Set<Venta>().AsNoTracking().Where(x => x.Estado == EstadoVenta.Registrada
            && x.Fecha >= inicioMesUtc && x.Fecha < finDiaUtc);
        var importes = ventas.Select(x => new { x.Fecha, x.Total });
        // PostgreSQL agrega en el servidor. SQLite no soporta SUM(decimal).
        var totales = db.Database.IsNpgsql()
            ? await importes.GroupBy(x => 1).Select(g => new Totales(
                g.Where(x => x.Fecha >= inicioDiaUtc).Sum(x => x.Total),
                g.Count(x => x.Fecha >= inicioDiaUtc), g.Sum(x => x.Total), g.Count())).SingleOrDefaultAsync(ct)
            : (await importes.ToListAsync(ct)).GroupBy(x => 1).Select(g => new Totales(
                g.Where(x => x.Fecha >= inicioDiaUtc).Sum(x => x.Total),
                g.Count(x => x.Fecha >= inicioDiaUtc), g.Sum(x => x.Total), g.Count())).SingleOrDefault();

        var lineas = from venta in ventas
                     join detalle in db.Set<DetalleVenta>().AsNoTracking() on venta.Id equals detalle.VentaId
                     join producto in db.Set<Producto>().AsNoTracking() on detalle.ProductoId equals producto.Id
                     select new { producto.Id, producto.CostoPromedio, detalle.Cantidad, detalle.Subtotal };
        var margenes = db.Database.IsNpgsql()
            ? await lineas.GroupBy(x => new { x.Id, x.CostoPromedio }).Select(g => new Margen(
                g.Sum(x => x.Subtotal), g.Sum(x => x.Cantidad) * g.Key.CostoPromedio)).ToListAsync(ct)
            : (await lineas.ToListAsync(ct)).GroupBy(x => new { x.Id, x.CostoPromedio }).Select(g => new Margen(
                g.Sum(x => x.Subtotal), g.Sum(x => x.Cantidad) * g.Key.CostoPromedio)).ToList();

        var stock = from producto in db.Set<Producto>().AsNoTracking()
                    where producto.Activo
                    join inventario in db.Set<Inventario>().AsNoTracking().Where(x => x.AlmacenId == Almacen.PrincipalId)
                        on producto.Id equals inventario.ProductoId into existencias
                    from inventario in existencias.DefaultIfEmpty()
                    select new { producto.StockMinimo, Cantidad = inventario == null ? 0m : inventario.Cantidad };
        var inventarioResumen = await stock.GroupBy(x => 1).Select(g => new InventarioResumen(
            g.Count(), g.Count(x => x.Cantidad <= x.StockMinimo), g.Count(x => x.Cantidad == 0))).SingleOrDefaultAsync(ct);
        var resultado = new ResumenDashboard(fecha, totales?.Hoy ?? 0, totales?.CantidadHoy ?? 0, totales?.Mes ?? 0,
            totales?.CantidadMes ?? 0, totales is { CantidadMes: > 0 }
                ? decimal.Round(totales.Mes / totales.CantidadMes, 2, MidpointRounding.AwayFromZero) : 0,
            margenes.Sum(x => x.Ingreso - decimal.Round(x.Costo, 2, MidpointRounding.AwayFromZero)),
            inventarioResumen?.Activos ?? 0, inventarioResumen?.BajoMinimo ?? 0, inventarioResumen?.SinStock ?? 0);
        if (snapshot is not null) await snapshot.CommitAsync(ct);
        return resultado;
    }

    private sealed record Totales(decimal Hoy, int CantidadHoy, decimal Mes, int CantidadMes);
    private sealed record Margen(decimal Ingreso, decimal Costo);
    private sealed record InventarioResumen(int Activos, int BajoMinimo, int SinStock);
}
