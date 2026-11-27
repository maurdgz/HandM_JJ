using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

/// <remarks>
/// El filtro, los joins y la proyección se ejecutan en la base. SQLite (pruebas) no suma decimales
/// en el servidor, así que GroupBy/Sum/OrderByDescending/Take se aplican sobre las filas proyectadas,
/// igual que el saldo del kardex.
/// </remarks>
internal sealed class ReporteRepository(AppDbContext db) : IReporteRepository
{
    // Perú no usa horario de verano.
    private static readonly TimeSpan HoraPeru = TimeSpan.FromHours(-5);

    private IQueryable<Venta> VentasRegistradas(DateTime desdeUtc, DateTime hastaUtc)
        => db.Set<Venta>()
            .AsNoTracking()
            .Where(x => x.Estado == EstadoVenta.Registrada
                && x.Fecha >= desdeUtc
                && x.Fecha < hastaUtc);

    public async Task<IReadOnlyList<VentasPorPeriodo>> VentasPorPeriodo(
        DateTime desdeUtc,
        DateTime hastaUtc,
        AgrupacionPeriodo agrupacion,
        CancellationToken ct)
    {
        var filas = await VentasRegistradas(desdeUtc, hastaUtc)
            .Select(x => new { x.Fecha, x.Total })
            .ToListAsync(ct);

        var formato = agrupacion == AgrupacionPeriodo.Mes ? "yyyy-MM" : "yyyy-MM-dd";

        return filas
            .GroupBy(x => (x.Fecha + HoraPeru).ToString(formato))
            .Select(g => new VentasPorPeriodo(g.Key, g.Count(), g.Sum(x => x.Total)))
            .OrderBy(x => x.Periodo, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<IReadOnlyList<VentasPorVendedor>> VentasPorVendedor(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct)
    {
        var filas = await (
            from venta in VentasRegistradas(desdeUtc, hastaUtc)
            join empleado in db.Set<Empleado>().AsNoTracking()
                on venta.EmpleadoId equals empleado.Id
            select new
            {
                venta.EmpleadoId,
                Vendedor = empleado.Nombre + " " + empleado.Apellidos,
                venta.Total
            })
            .ToListAsync(ct);

        return filas
            .GroupBy(x => new { x.EmpleadoId, x.Vendedor })
            .Select(g =>
            {
                var total = g.Sum(x => x.Total);
                var ventas = g.Count();
                return new VentasPorVendedor(
                    g.Key.EmpleadoId,
                    g.Key.Vendedor.Trim(),
                    ventas,
                    total,
                    decimal.Round(total / ventas, 2, MidpointRounding.AwayFromZero));
            })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Vendedor, StringComparer.Ordinal)
            .ToArray();
    }
}
