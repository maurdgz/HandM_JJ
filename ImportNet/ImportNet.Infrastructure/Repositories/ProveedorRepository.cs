using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class ProveedorRepository(AppDbContext context)
    : GenericRepository<Proveedor>(context), IProveedorRepository
{
    public async Task<IReadOnlyList<Proveedor>> ListarAsync(string? texto, CancellationToken ct = default)
    {
        var query = _dbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(texto))
            query = query.Where(p => p.Nombre.ToLower().Contains(texto.ToLower()));

        return await query
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Id)
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<int>> ObtenerProductosIdsAsync(int proveedorId, CancellationToken ct = default)
        => await _context.Set<ProveedorProducto>()
            .Where(pp => pp.ProveedorId == proveedorId)
            .OrderBy(pp => pp.ProductoId)
            .Select(pp => pp.ProductoId)
            .ToListAsync(ct);

    public async Task GuardarConProductosAsync(Proveedor proveedor, IReadOnlyList<int> productos, CancellationToken ct = default)
    {
        var existentes = await _context.Set<Producto>().CountAsync(p => productos.Contains(p.Id), ct);
        if (existentes != productos.Count)
            throw new ReglaNegocioException("Producto inexistente o repetido.");

        if (proveedor.Id == 0)
            Agregar(proveedor);

        var actuales = await _context.Set<ProveedorProducto>()
            .Where(pp => pp.ProveedorId == proveedor.Id)
            .ToListAsync(ct);

        var quitar = actuales.Where(pp => !productos.Contains(pp.ProductoId)).ToList();
        _context.Set<ProveedorProducto>().RemoveRange(quitar);

        var idsActuales = actuales.Select(pp => pp.ProductoId).ToHashSet();
        foreach (var productoId in productos.Where(id => !idsActuales.Contains(id)))
            proveedor.Suministros.Add(new ProveedorProducto { ProductoId = productoId });
    }

    public async Task EliminarProveedorAsync(int id, CancellationToken ct = default)
    {
        var proveedor = await ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Proveedor no encontrado.");

        var tieneCompras = await _context.Set<Compra>().AnyAsync(c => c.ProveedorId == id, ct);
        var tieneMovimientos = await _context.Set<MovimientoInventario>().AnyAsync(m => m.ProveedorId == id, ct);
        if (tieneCompras || tieneMovimientos)
            throw new ConflictoException("Proveedor con movimientos o compras.");

        var suministros = await _context.Set<ProveedorProducto>().Where(pp => pp.ProveedorId == id).ToListAsync(ct);
        _context.Set<ProveedorProducto>().RemoveRange(suministros);
        Eliminar(proveedor);
    }
}
