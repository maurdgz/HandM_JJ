using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class VentaRepository(AppDbContext db) : IVentaRepository
{
    public async Task<Pagina<ResumenVenta>> Historial(
        DateTime? desdeUtc,
        DateTime? hastaUtc,
        int? empleadoId,
        int pagina,
        int tamano,
        CancellationToken ct)
    {
        var ventas = db.Set<Venta>().AsNoTracking();

        if (desdeUtc is { } desde)
            ventas = ventas.Where(x => x.Fecha >= desde);

        if (hastaUtc is { } hasta)
            ventas = ventas.Where(x => x.Fecha < hasta);

        if (empleadoId is { } empleado)
            ventas = ventas.Where(x => x.EmpleadoId == empleado);

        var count = await ventas.CountAsync(ct);

        var filas = await (
            from venta in ventas
            join vendedor in db.Set<Empleado>().AsNoTracking()
                on venta.EmpleadoId equals vendedor.Id
            join cliente in db.Set<Cliente>().AsNoTracking()
                on venta.ClienteId equals (int?)cliente.Id into clientes
            from cliente in clientes.DefaultIfEmpty()
            orderby venta.Fecha descending, venta.Id descending
            select new
            {
                venta.Id,
                venta.Numero,
                venta.Fecha,
                venta.Estado,
                venta.EmpleadoId,
                Vendedor = vendedor.Nombre + " " + vendedor.Apellidos,
                Cliente = cliente == null ? "Cliente varios" : cliente.Nombre,
                Lineas = db.Set<DetalleVenta>().Count(d => d.VentaId == venta.Id),
                venta.Total
            })
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(ct);

        var items = filas
            .Select(x => new ResumenVenta(
                x.Id,
                x.Numero,
                x.Fecha,
                x.Estado.ToString(),
                x.EmpleadoId,
                x.Vendedor.Trim(),
                x.Cliente,
                x.Lineas,
                x.Total))
            .ToArray();

        return new Pagina<ResumenVenta>(items, count, pagina, tamano);
    }

    public async Task<NotaVenta?> Nota(int ventaId, CancellationToken ct)
    {
        var cabecera = await (
            from venta in db.Set<Venta>().AsNoTracking()
            join vendedor in db.Set<Empleado>().AsNoTracking()
                on venta.EmpleadoId equals vendedor.Id
            join cliente in db.Set<Cliente>().AsNoTracking()
                on venta.ClienteId equals (int?)cliente.Id into clientes
            from cliente in clientes.DefaultIfEmpty()
            where venta.Id == ventaId
            select new
            {
                venta.Id,
                venta.Numero,
                venta.Fecha,
                venta.Estado,
                venta.EmpleadoId,
                Vendedor = vendedor.Nombre + " " + vendedor.Apellidos,
                TipoDocumento = cliente == null ? (TipoDocumento?)null : cliente.TipoDocumento,
                Documento = cliente == null ? "" : cliente.NumeroDocumento,
                Cliente = cliente == null ? "Cliente varios" : cliente.Nombre,
                venta.Subtotal,
                venta.Descuento,
                venta.Total,
                venta.MotivoAnulacion,
                venta.FechaAnulacion
            })
            .SingleOrDefaultAsync(ct);

        if (cabecera is null)
            return null;

        var lineas = await (
            from detalle in db.Set<DetalleVenta>().AsNoTracking()
            join producto in db.Set<Producto>().AsNoTracking()
                on detalle.ProductoId equals producto.Id
            where detalle.VentaId == ventaId
            orderby detalle.Id
            select new LineaNotaVenta(
                detalle.ProductoId,
                producto.Codigo,
                producto.Nombre,
                producto.UnidadMedida,
                detalle.Cantidad,
                detalle.PrecioUnitario,
                detalle.Descuento,
                detalle.Subtotal))
            .ToListAsync(ct);

        return new NotaVenta(
            cabecera.Id,
            cabecera.Numero,
            cabecera.Fecha,
            cabecera.Estado.ToString(),
            cabecera.EmpleadoId,
            cabecera.Vendedor.Trim(),
            cabecera.TipoDocumento?.ToString().ToUpperInvariant() ?? "",
            cabecera.Documento,
            cabecera.Cliente,
            cabecera.Subtotal,
            cabecera.Descuento,
            cabecera.Total,
            cabecera.MotivoAnulacion,
            cabecera.FechaAnulacion,
            lineas);
    }
}
