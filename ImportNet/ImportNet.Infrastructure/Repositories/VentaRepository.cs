using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class VentaRepository(AppDbContext context) : IVentaRepository
{
    public async Task<Pagina<ResumenVenta>> HistorialAsync(
        DateTime? desdeUtc,
        DateTime? hastaUtc,
        int? empleadoId,
        int pagina,
        int tamano,
        CancellationToken ct = default)
    {
        var query = context.Set<Venta>().AsNoTracking();

        if (desdeUtc != null)
            query = query.Where(v => v.Fecha >= desdeUtc);

        if (hastaUtc != null)
            query = query.Where(v => v.Fecha < hastaUtc);

        if (empleadoId != null)
            query = query.Where(v => v.EmpleadoId == empleadoId);

        var count = await query.CountAsync(ct);

        var ventas = await query
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(v => new
            {
                v.Id,
                v.Numero,
                v.Fecha,
                v.Estado,
                v.EmpleadoId,
                EmpleadoNombre = v.Empleado.Nombre,
                EmpleadoApellidos = v.Empleado.Apellidos,
                ClienteNombre = v.Cliente == null ? "Cliente varios" : v.Cliente.Nombre,
                Lineas = v.Detalles.Count,
                v.Total
            })
            .ToListAsync(ct);

        var items = ventas
            .Select(v => new ResumenVenta(
                v.Id,
                v.Numero,
                v.Fecha,
                v.Estado.ToString(),
                v.EmpleadoId,
                (v.EmpleadoNombre + " " + v.EmpleadoApellidos).Trim(),
                v.ClienteNombre,
                v.Lineas,
                v.Total))
            .ToList();

        return new Pagina<ResumenVenta>(items, count, pagina, tamano);
    }

    public async Task<NotaVenta?> ObtenerNotaAsync(int ventaId, CancellationToken ct = default)
    {
        var venta = await context.Set<Venta>()
            .AsNoTracking()
            .Include(v => v.Empleado)
            .Include(v => v.Cliente)
            .Include(v => v.Detalles)
            .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(v => v.Id == ventaId, ct);

        if (venta == null)
            return null;

        var lineas = venta.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new LineaNotaVenta(
                d.ProductoId,
                d.Producto.Codigo,
                d.Producto.Nombre,
                d.Producto.UnidadMedida,
                d.Cantidad,
                d.PrecioUnitario,
                d.Descuento,
                d.Subtotal))
            .ToList();

        return new NotaVenta(
            venta.Id,
            venta.Numero,
            venta.Fecha,
            venta.Estado.ToString(),
            venta.EmpleadoId,
            (venta.Empleado.Nombre + " " + venta.Empleado.Apellidos).Trim(),
            venta.Cliente == null ? "" : venta.Cliente.TipoDocumento.ToString().ToUpperInvariant(),
            venta.Cliente?.NumeroDocumento ?? "",
            venta.Cliente?.Nombre ?? "Cliente varios",
            venta.Subtotal,
            venta.Descuento,
            venta.Total,
            venta.MotivoAnulacion,
            venta.FechaAnulacion,
            lineas);
    }

    public Task<Venta?> ObtenerConDetallesAsync(int ventaId, CancellationToken ct = default)
        => context.Set<Venta>()
            .Include(v => v.Detalles.OrderBy(d => d.Id))
            .FirstOrDefaultAsync(v => v.Id == ventaId, ct);

    public async Task<int> SiguienteNumeroAsync(CancellationToken ct = default)
    {
        var ultimo = await context.Set<Venta>().MaxAsync(v => (int?)v.Numero, ct);
        return (ultimo ?? 0) + 1;
    }

    public void Agregar(Venta venta) => context.Set<Venta>().Add(venta);
}
