using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class ProductoRepository(AppDbContext context)
    : GenericRepository<Producto>(context), IProductoRepository
{
    public override async Task<IReadOnlyList<Producto>> ListarAsync(CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Codigo)
            .ToListAsync(ct);

    public async Task<Pagina<Producto>> BuscarAsync(
        string? texto,
        int? categoriaId,
        bool stockBajo,
        int pagina,
        int tamano,
        bool? activo,
        CancellationToken ct = default)
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

    public async Task<IReadOnlyDictionary<int, Producto>> ObtenerPorIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return new Dictionary<int, Producto>();

        return await _dbSet
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
    }

    public Task<bool> ExisteCodigoAsync(string codigo, int excluirId, CancellationToken ct = default)
        => _dbSet.AnyAsync(p => p.Codigo == codigo && p.Id != excluirId, ct);

    public async Task<bool> TieneReferenciasAsync(int id, CancellationToken ct = default)
    {
        return await _context.Set<Inventario>().AnyAsync(i => i.ProductoId == id, ct)
            || await _context.Set<MovimientoInventario>().AnyAsync(m => m.ProductoId == id, ct)
            || await _context.Set<DetalleCompra>().AnyAsync(d => d.ProductoId == id, ct)
            || await _context.Set<DetalleVenta>().AnyAsync(d => d.ProductoId == id, ct)
            || await _context.Set<ProveedorProducto>().AnyAsync(pp => pp.ProductoId == id, ct);
    }
}
