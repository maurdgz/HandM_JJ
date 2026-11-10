using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class ProductoRepository : GenericRepository<Producto>, IProductoRepository
{
    public ProductoRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<IReadOnlyList<Producto>> Listar(CancellationToken ct)
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Codigo)
            .ToListAsync(ct);
    }

    public Task<bool> ExisteCodigo(string codigo, int excluirId, CancellationToken ct)
    {
        return _dbSet.AnyAsync(p => p.Codigo == codigo && p.Id != excluirId, ct);
    }

    public async Task<bool> TieneReferencias(int id, CancellationToken ct)
    {
        return await _context.Set<Inventario>().AnyAsync(i => i.ProductoId == id, ct)
            || await _context.Set<MovimientoInventario>().AnyAsync(m => m.ProductoId == id, ct)
            || await _context.Set<DetalleVenta>().AnyAsync(d => d.ProductoId == id, ct)
            || await _context.Set<ProveedorProducto>().AnyAsync(pp => pp.ProductoId == id, ct);
    }
}
