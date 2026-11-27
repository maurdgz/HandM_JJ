using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Ventas;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ImportNet.Infrastructure.Services;

internal sealed class AnulacionVentas(
    AppDbContext db,
    IUsuarioActual actor) : IAnulacionVentas
{
    public async Task<VentaAnuladaDto> Anular(
        int ventaId,
        string motivo,
        int empleadoId,
        CancellationToken ct)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(ct);

        try
        {
            if (db.Database.IsNpgsql())
            {
                // Las anulaciones de una misma venta se procesan en orden.
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(480071127, {ventaId})",
                    ct);
            }

            await actor.ExigirAdministrador(ct);

            var venta = await db.Set<Venta>()
                .SingleOrDefaultAsync(
                    v => v.Id == ventaId, ct)
                ?? throw new NoEncontradoException(
                    "Venta no encontrada.");

            if (venta.Estado == EstadoVenta.Anulada)
            {
                throw new ConflictoException(
                    "La venta ya está anulada.");
            }

            var salidas = await db.Set<MovimientoInventario>()
                .Where(m =>
                    m.VentaId == ventaId &&
                    m.Tipo == TipoMovimiento.Salida)
                .OrderBy(m => m.ProductoId)
                .ThenBy(m => m.AlmacenId)
                .ToListAsync(ct);

            var detalles = await db.Set<DetalleVenta>()
                .Where(d => d.VentaId == ventaId)
                .ToListAsync(ct);

            if (salidas.Count == 0 || detalles.Count == 0)
            {
                throw new ConflictoException(
                    "La venta no tiene detalle y salidas verificables.");
            }

            var cantidadesDetalle = detalles
                .GroupBy(d => d.ProductoId)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.Sum(d => d.Cantidad));

            var cantidadesSalida = salidas
                .GroupBy(m => m.ProductoId)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.Sum(m => m.Cantidad));

            var cantidadesInvalidas =
                salidas.Any(m => m.Cantidad <= 0) ||
                cantidadesDetalle.Count != cantidadesSalida.Count ||
                cantidadesDetalle.Any(item =>
                    !cantidadesSalida.TryGetValue(
                        item.Key, out var cantidad) ||
                    cantidad != item.Value);

            if (cantidadesInvalidas)
            {
                throw new ConflictoException(
                    "Las salidas no coinciden con las cantidades vendidas.");
            }

            var tieneReposicion =
                await db.Set<MovimientoInventario>()
                    .AnyAsync(m =>
                        m.VentaId == ventaId &&
                        m.Tipo == TipoMovimiento.Entrada,
                        ct);

            if (tieneReposicion)
            {
                throw new ConflictoException(
                    "La venta ya tiene una reposición registrada.");
            }

            venta.Anular(motivo);
            var reposiciones = new List<StockRepuestoDto>();

            foreach (var salida in salidas)
            {
                var inventario = await db.Set<Inventario>()
                    .SingleOrDefaultAsync(i =>
                        i.ProductoId == salida.ProductoId &&
                        i.AlmacenId == salida.AlmacenId,
                        ct)
                    ?? throw new ConflictoException(
                        "No se encontró el inventario de una línea vendida.");

                inventario.Cantidad += salida.Cantidad;
                reposiciones.Add(new StockRepuestoDto(salida.ProductoId, salida.Cantidad, inventario.Cantidad));

                db.Add(new MovimientoInventario
                {
                    ProductoId = salida.ProductoId,
                    AlmacenId = salida.AlmacenId,
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = salida.Cantidad,
                    EmpleadoId = empleadoId,
                    VentaId = venta.Id,
                    Fecha = venta.FechaAnulacion!.Value,
                    Observacion = venta.MotivoAnulacion
                });
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new VentaAnuladaDto(venta.Id, venta.Numero, venta.Estado.ToString(),
                venta.MotivoAnulacion!, venta.FechaAnulacion!.Value, reposiciones);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoException(
                "El inventario cambió concurrentemente. Recargue e intente nuevamente.");
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: "23505" or "23503"
            })
        {
            throw new ConflictoException(
                "La reposición tiene referencias inválidas o ya fue registrada.");
        }
    }
}
