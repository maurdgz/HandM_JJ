using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class MovimientoRepository : IMovimientoRepository
{
    private readonly AppDbContext _context;

    public MovimientoRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Agregar(MovimientoInventario movimiento)
    {
        _context.Set<MovimientoInventario>().Add(movimiento);
    }

    public Task<bool> TieneEntradasDeCompra(int compraId, CancellationToken ct)
    {
        return _context.Set<MovimientoInventario>()
            .AnyAsync(m => m.CompraId == compraId && m.Tipo == TipoMovimiento.Entrada, ct);
    }

    public async Task<KardexProducto?> Kardex(int productoId, DateTime? desdeUtc, DateTime? hastaUtc, CancellationToken ct)
    {
        var producto = await _context.Set<Producto>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productoId, ct);

        if (producto == null)
            return null;

        var inventario = await _context.Set<Inventario>()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ProductoId == productoId && i.AlmacenId == Almacen.PrincipalId, ct);

        var stockActual = inventario == null ? 0m : inventario.Cantidad;

        var query = _context.Set<MovimientoInventario>()
            .AsNoTracking()
            .Include(m => m.Empleado)
            .Include(m => m.Venta)
            .Where(m => m.ProductoId == productoId && m.AlmacenId == Almacen.PrincipalId);

        if (hastaUtc != null)
            query = query.Where(m => m.Fecha < hastaUtc);

        var movimientos = await query
            .OrderBy(m => m.Fecha)
            .ThenBy(m => m.Id)
            .ToListAsync(ct);

        var saldoInicial = 0m;
        var saldo = 0m;
        var lineas = new List<LineaKardex>();

        foreach (var m in movimientos)
        {
            var cantidad = m.Tipo == TipoMovimiento.Salida ? -m.Cantidad : m.Cantidad;

            if (desdeUtc != null && m.Fecha < desdeUtc)
            {
                saldoInicial += cantidad;
                saldo = saldoInicial;
                continue;
            }

            saldo += cantidad;

            lineas.Add(new LineaKardex(
                m.Id,
                m.Fecha,
                m.Tipo.ToString(),
                Referencia(m),
                m.CompraId,
                m.VentaId,
                cantidad > 0 ? cantidad : 0m,
                cantidad < 0 ? -cantidad : 0m,
                saldo,
                (m.Empleado.Nombre + " " + m.Empleado.Apellidos).Trim(),
                m.MotivoAjuste == null ? null : AjusteInventario.Etiqueta(m.MotivoAjuste.Value),
                m.Observacion));
        }

        return new KardexProducto(
            producto.Id,
            producto.Codigo,
            producto.Nombre,
            producto.UnidadMedida,
            saldoInicial,
            lineas.Sum(l => l.Entrada),
            lineas.Sum(l => l.Salida),
            saldo,
            stockActual,
            lineas);
    }

    private static string Referencia(MovimientoInventario m)
    {
        if (m.CompraId != null)
            return $"Compra #{m.CompraId}";

        if (m.Venta != null)
            return m.Tipo == TipoMovimiento.Entrada ? $"Anulación venta N° {m.Venta.Numero}" : $"Venta N° {m.Venta.Numero}";

        if (m.Tipo == TipoMovimiento.Ajuste && m.MotivoAjuste != null)
            return $"Ajuste: {AjusteInventario.Etiqueta(m.MotivoAjuste.Value)}";

        return m.Tipo.ToString();
    }
}
