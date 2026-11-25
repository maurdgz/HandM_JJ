using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Domain.ReadModels;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class VentaRepository(AppDbContext context)
    : GenericRepository<Venta>(context), IVentaRepository
{
    private IQueryable<VentaResumen> Consulta(
        int? empleadoId, int? id = null,
        DateTime? desdeUtc = null, DateTime? hastaUtcExclusivo = null)
    {
        var ventas = _context.Set<Venta>().AsNoTracking();

        if (empleadoId.HasValue)
        {
            ventas = ventas.Where(
                v => v.EmpleadoId == empleadoId.Value);
        }

        if (id.HasValue) ventas = ventas.Where(v => v.Id == id.Value);
        if (desdeUtc.HasValue) ventas = ventas.Where(v => v.Fecha >= desdeUtc.Value);
        if (hastaUtcExclusivo.HasValue) ventas = ventas.Where(v => v.Fecha < hastaUtcExclusivo.Value);

        return
            from v in ventas
            join e in _context.Set<Empleado>()
                on v.EmpleadoId equals e.Id
            join c in _context.Set<Cliente>()
                on v.ClienteId equals (int?)c.Id into clientes
            from c in clientes.DefaultIfEmpty()
            orderby v.Fecha descending, v.Id descending
            select new VentaResumen(
                v.Id,
                v.Numero,
                v.Fecha,
                v.EmpleadoId,
                e.Nombre + " " + e.Apellidos,
                c == null ? "Cliente varios" : c.Nombre,
                c == null ? null : c.NumeroDocumento,
                v.Estado == EstadoVenta.Registrada
                    ? "Registrada"
                    : "Anulada",
                v.Subtotal,
                v.Descuento,
                v.Total,
                v.MotivoAnulacion);
    }

    public async Task<IReadOnlyList<VentaResumen>> Historial(
        DateTime desdeUtc,
        DateTime hastaUtcExclusivo,
        int? empleadoId,
        CancellationToken ct)
    {
        var ventas = await Consulta(empleadoId, desdeUtc: desdeUtc, hastaUtcExclusivo: hastaUtcExclusivo)
            .ToListAsync(ct);
        return ventas.Select(ConFechaUtc).ToArray();
    }

    public async Task<NotaVenta?> Nota(
        int id,
        int? empleadoId,
        CancellationToken ct)
    {
        var venta = await Consulta(empleadoId, id)
            .SingleOrDefaultAsync(ct);

        if (venta is null)
            return null;

        var lineas = await (
            from d in _context.Set<DetalleVenta>().AsNoTracking()
            join p in _context.Set<Producto>()
                on d.ProductoId equals p.Id
            where d.VentaId == id
            orderby d.Id
            select new VentaLinea(
                p.Codigo,
                p.Nombre,
                p.UnidadMedida,
                d.Cantidad,
                d.PrecioUnitario,
                d.Descuento,
                d.Subtotal))
            .ToListAsync(ct);

        return new NotaVenta(ConFechaUtc(venta), lineas);
    }

    private static VentaResumen ConFechaUtc(VentaResumen venta)
        => venta with { Fecha = DateTime.SpecifyKind(venta.Fecha, DateTimeKind.Utc) };
}
