using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class InventarioRepository(AppDbContext context)
    : GenericRepository<Inventario>(context), IInventarioRepository
{
    public Task<Inventario?> ObtenerAsync(int productoId, int almacenId, CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(i => i.ProductoId == productoId && i.AlmacenId == almacenId, ct);

    public async Task<IReadOnlyDictionary<int, Inventario>> ObtenerPorProductosAsync(
        IReadOnlyCollection<int> productoIds,
        int almacenId,
        CancellationToken ct = default)
    {
        if (productoIds.Count == 0)
            return new Dictionary<int, Inventario>();

        return await _dbSet
            .Where(i => i.AlmacenId == almacenId && productoIds.Contains(i.ProductoId))
            .ToDictionaryAsync(i => i.ProductoId, ct);
    }

    public async Task<Pagina<StockProducto>> BuscarAsync(
        string? texto,
        bool stockBajo,
        int pagina,
        int tamano,
        CancellationToken ct = default)
    {
        var query = _context.Set<Producto>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var busqueda = texto.Trim().ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(busqueda) || p.Codigo.ToLower().Contains(busqueda));
        }

        if (stockBajo)
        {
            query = query.Where(p =>
                p.Inventarios.Where(i => i.AlmacenId == Almacen.PrincipalId).Sum(i => i.Cantidad) <= p.StockMinimo);
        }

        var count = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(p => new StockProducto(
                p.Id,
                p.Codigo,
                p.Nombre,
                p.UnidadMedida,
                Almacen.PrincipalId,
                p.Inventarios.Where(i => i.AlmacenId == Almacen.PrincipalId).Sum(i => i.Cantidad),
                p.StockMinimo,
                p.Activo))
            .ToListAsync(ct);

        return new Pagina<StockProducto>(items, count, pagina, tamano);
    }
}
