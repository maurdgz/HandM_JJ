using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class VentaRepository : IVentaRepository
{
    private readonly AppDbContext _context;

    public VentaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Pagina<ResumenVenta>> Historial(DateTime? desdeUtc, DateTime? hastaUtc, int? empleadoId,
        int pagina, int tamano, CancellationToken ct)
    {
        var query = _context.Set<Venta>().AsNoTracking();

        if (desdeUtc != null)
            query = query.Where(v => v.Fecha >= desdeUtc);

        if (hastaUtc != null)
            query = query.Where(v => v.Fecha < hastaUtc);

        if (empleadoId != null)
            query = query.Where(v => v.EmpleadoId == empleadoId);

        var count = await query.CountAsync(ct);

        var ventas = await query
            .Include(v => v.Empleado)
            .Include(v => v.Cliente)
            .Include(v => v.Detalles)
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(ct);

        var items = ventas
            .Select(v => new ResumenVenta(
                v.Id,
                v.Numero,
                v.Fecha,
                v.Estado.ToString(),
                v.EmpleadoId,
                (v.Empleado.Nombre + " " + v.Empleado.Apellidos).Trim(),
                v.Cliente == null ? "Cliente varios" : v.Cliente.Nombre,
                v.Detalles.Count,
                v.Total))
            .ToList();

        return new Pagina<ResumenVenta>(items, count, pagina, tamano);
    }

    public async Task<NotaVenta?> Nota(int ventaId, CancellationToken ct)
    {
        var venta = await _context.Set<Venta>()
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
            venta.Cliente == null ? "" : venta.Cliente.NumeroDocumento,
            venta.Cliente == null ? "Cliente varios" : venta.Cliente.Nombre,
            venta.Subtotal,
            venta.Descuento,
            venta.Total,
            venta.MotivoAnulacion,
            venta.FechaAnulacion,
            lineas);
    }

    public Task<Venta?> ObtenerConDetalles(int ventaId, CancellationToken ct)
    {
        return _context.Set<Venta>()
            .Include(v => v.Detalles.OrderBy(d => d.Id))
            .FirstOrDefaultAsync(v => v.Id == ventaId, ct);
    }

    public async Task<int> SiguienteNumero(CancellationToken ct)
    {
        var ultimo = await _context.Set<Venta>().MaxAsync(v => (int?)v.Numero, ct);
        return (ultimo ?? 0) + 1;
    }

    public void Agregar(Venta venta)
    {
        _context.Set<Venta>().Add(venta);
    }
}
