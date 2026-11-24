using ImportNet.Application.UseCases.Ventas;
using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class RegistroVenta(AppDbContext db) : IRegistroVenta
{
    public async Task<VentaRegistradaDto> Registrar(int empleadoId, int? clienteId, IReadOnlyList<LineaVentaEntrada> lineas, CancellationToken ct)
    {
        if (lineas is null || lineas.Count == 0 || lineas.Count > 100)
            throw new ReglaNegocioException("La venta debe tener entre 1 y 100 líneas.");
        if (lineas.Any(x => x.ProductoId <= 0 || x.Cantidad <= 0 || x.Cantidad > 1000000 || decimal.Round(x.Cantidad, 3) != x.Cantidad || x.Descuento < 0 || decimal.Round(x.Descuento, 2) != x.Descuento))
            throw new ReglaNegocioException("Producto, cantidad o descuento inválido.");

        var agrupadas = lineas.GroupBy(x => x.ProductoId).Select(g => new LineaVentaEntrada(g.Key, g.Sum(x => x.Cantidad), g.Sum(x => x.Descuento))).ToArray();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var empleadoValido = await db.Set<Empleado>().AnyAsync(x => x.Id == empleadoId && x.Activo, ct);
            if (!empleadoValido) throw new AccesoNoPermitidoException(403);
            var cliente = clienteId ?? 1;
            if (!await db.Set<Cliente>().AnyAsync(x => x.Id == cliente, ct)) throw new NoEncontradoException("Cliente no encontrado.");
            var productos = await db.Set<Producto>().Where(x => agrupadas.Select(l => l.ProductoId).Contains(x.Id) && x.Activo).ToDictionaryAsync(x => x.Id, ct);
            if (productos.Count != agrupadas.Length) throw new NoEncontradoException("Hay productos inexistentes o inactivos.");
            var inventarios = new Dictionary<int, Inventario>();
            foreach (var linea in agrupadas)
            {
                if (linea.Descuento > productos[linea.ProductoId].PrecioVenta * linea.Cantidad * 0.5m)
                    throw new ReglaNegocioException("El descuento máximo por línea es 50%.");
                var inventario = await db.Set<Inventario>().SingleOrDefaultAsync(x => x.ProductoId == linea.ProductoId && x.AlmacenId == Almacen.PrincipalId, ct);
                if (inventario is null || inventario.Cantidad < linea.Cantidad) throw new ConflictoException("Stock insuficiente para completar la venta.");
                inventarios.Add(linea.ProductoId, inventario);
            }
            var subtotal = agrupadas.Sum(x => decimal.Round(productos[x.ProductoId].PrecioVenta * x.Cantidad, 2, MidpointRounding.AwayFromZero));
            var descuento = agrupadas.Sum(x => x.Descuento);
            var numero = (await db.Set<Venta>().MaxAsync(x => (int?)x.Numero, ct) ?? 0) + 1;
            var venta = new Venta(numero, empleadoId, cliente, subtotal, descuento, DateTime.UtcNow);
            db.Set<Venta>().Add(venta);
            await db.SaveChangesAsync(ct);
            var detalles = new List<DetalleVenta>();
            foreach (var linea in agrupadas)
            {
                var producto = productos[linea.ProductoId];
                var inventario = inventarios[linea.ProductoId];
                inventario.Cantidad -= linea.Cantidad; // PostgreSQL xmin protects this update from concurrent sales.
                var detalle = new DetalleVenta(linea.ProductoId, linea.Cantidad, producto.PrecioVenta, linea.Descuento) { VentaId = venta.Id };
                detalles.Add(detalle);
                db.Set<MovimientoInventario>().Add(new MovimientoInventario { ProductoId = linea.ProductoId, AlmacenId = Almacen.PrincipalId, Tipo = TipoMovimiento.Salida, Cantidad = linea.Cantidad, EmpleadoId = empleadoId, VentaId = venta.Id, Fecha = DateTime.UtcNow });
            }
            db.Set<DetalleVenta>().AddRange(detalles);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new(venta.Id, venta.Numero, venta.Fecha, venta.Subtotal, venta.Descuento, venta.Total, detalles.Select(x => new DetalleVentaDto(x.ProductoId, x.Cantidad, x.PrecioUnitario, x.Descuento, x.Subtotal)).ToArray());
        }
        catch (DbUpdateConcurrencyException) { throw new ConflictoException("El stock cambió durante la venta; vuelva a consultar e intente otra vez."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" }) { throw new ConflictoException("Conflicto de numeración o venta duplicada."); }
    }
}
