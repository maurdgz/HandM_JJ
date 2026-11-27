using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class ProveedorRepository : IProveedorRepository
{
    private readonly AppDbContext _context;

    public ProveedorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Proveedor>> Proveedores(string? texto, CancellationToken ct)
    {
        var query = _context.Set<Proveedor>().AsNoTracking();

        if (texto != null)
            query = query.Where(p => p.Nombre.ToLower().Contains(texto.ToLower()));

        return await query
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Id)
            .Take(100)
            .ToListAsync(ct);
    }

    public Task<Proveedor?> Proveedor(int id, CancellationToken ct)
    {
        return _context.Set<Proveedor>().FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<IReadOnlyList<int>> Productos(int proveedorId, CancellationToken ct)
    {
        return await _context.Set<ProveedorProducto>()
            .Where(pp => pp.ProveedorId == proveedorId)
            .OrderBy(pp => pp.ProductoId)
            .Select(pp => pp.ProductoId)
            .ToListAsync(ct);
    }

    public async Task GuardarProveedor(Proveedor proveedor, IReadOnlyList<int> productos, CancellationToken ct)
    {
        var existentes = await _context.Set<Producto>().CountAsync(p => productos.Contains(p.Id), ct);
        if (existentes != productos.Count)
            throw new ReglaNegocioException("Producto inexistente o repetido.");

        if (proveedor.Id == 0)
            _context.Set<Proveedor>().Add(proveedor);

        var actuales = await _context.Set<ProveedorProducto>()
            .Where(pp => pp.ProveedorId == proveedor.Id)
            .ToListAsync(ct);

        var quitar = actuales.Where(pp => !productos.Contains(pp.ProductoId)).ToList();
        _context.Set<ProveedorProducto>().RemoveRange(quitar);

        var idsActuales = actuales.Select(pp => pp.ProductoId).ToList();
        foreach (var productoId in productos.Where(id => !idsActuales.Contains(id)))
            proveedor.Suministros.Add(new ProveedorProducto { ProductoId = productoId });
    }

    public async Task EliminarProveedor(int id, CancellationToken ct)
    {
        var proveedor = await Proveedor(id, ct);
        if (proveedor == null)
            throw new NoEncontradoException("Proveedor no encontrado.");

        var tieneCompras = await _context.Set<Compra>().AnyAsync(c => c.ProveedorId == id, ct);
        var tieneMovimientos = await _context.Set<MovimientoInventario>().AnyAsync(m => m.ProveedorId == id, ct);
        if (tieneCompras || tieneMovimientos)
            throw new ConflictoException("El proveedor tiene historial.");

        var relaciones = await _context.Set<ProveedorProducto>().Where(pp => pp.ProveedorId == id).ToListAsync(ct);
        _context.Set<ProveedorProducto>().RemoveRange(relaciones);
        _context.Set<Proveedor>().Remove(proveedor);
    }
}
