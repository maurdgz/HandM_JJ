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

    public async Task<Pagina<Producto>> Buscar(string? texto, int? categoriaId, bool stockBajo, int pagina, int tamano, CancellationToken ct)
    {
        var q = _dbSet.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(texto)) { var t = texto.Trim().ToLower(); q = q.Where(p => p.Nombre.ToLower().Contains(t) || p.Codigo.ToLower().Contains(t)); }
        if (categoriaId.HasValue) q = q.Where(p => p.CategoriaId == categoriaId);
        if (stockBajo) q = q.Where(p => (_context.Set<Inventario>().Where(i => i.ProductoId == p.Id).Sum(i => (decimal?)i.Cantidad) ?? 0) <= p.StockMinimo);
        var count = await q.CountAsync(ct);
        return new(await q.OrderBy(p => p.Nombre).ThenBy(p => p.Id).Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct), count, pagina, tamano);
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
