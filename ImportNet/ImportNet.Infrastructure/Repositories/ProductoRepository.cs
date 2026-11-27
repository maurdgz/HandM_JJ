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

    public async Task<Pagina<Producto>> Buscar(string? texto, int? categoriaId, bool stockBajo, int pagina, int tamano,
        bool? activo, CancellationToken ct)
    {
        var query = _dbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var busqueda = texto.Trim().ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(busqueda) || p.Codigo.ToLower().Contains(busqueda));
        }

        if (activo != null)
            query = query.Where(p => p.Activo == activo);

        if (categoriaId != null)
            query = query.Where(p => p.CategoriaId == categoriaId);

        if (stockBajo)
            query = query.Where(p => p.Inventarios.Sum(i => i.Cantidad) <= p.StockMinimo);

        var count = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(ct);

        return new Pagina<Producto>(items, count, pagina, tamano);
    }

    public Task<bool> ExisteCodigo(string codigo, int excluirId, CancellationToken ct)
    {
        return _dbSet.AnyAsync(p => p.Codigo == codigo && p.Id != excluirId, ct);
    }

    public async Task<bool> TieneReferencias(int id, CancellationToken ct)
    {
        if (await _context.Set<Inventario>().AnyAsync(i => i.ProductoId == id, ct))
            return true;

        if (await _context.Set<MovimientoInventario>().AnyAsync(m => m.ProductoId == id, ct))
            return true;

        if (await _context.Set<DetalleCompra>().AnyAsync(d => d.ProductoId == id, ct))
            return true;

        if (await _context.Set<DetalleVenta>().AnyAsync(d => d.ProductoId == id, ct))
            return true;

        return await _context.Set<ProveedorProducto>().AnyAsync(pp => pp.ProductoId == id, ct);
    }
}
