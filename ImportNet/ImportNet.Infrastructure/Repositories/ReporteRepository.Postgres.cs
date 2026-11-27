using ImportNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed partial class ReporteRepository
{
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
