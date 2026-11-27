using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class InventarioRepository : IInventarioRepository
{
    private readonly AppDbContext _context;

    public InventarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Inventario?> Obtener(int productoId, int almacenId, CancellationToken ct)
    {
        return _context.Set<Inventario>().FirstOrDefaultAsync(i => i.ProductoId == productoId && i.AlmacenId == almacenId, ct);
    }

    public void Agregar(Inventario inventario)
    {
        _context.Set<Inventario>().Add(inventario);
    }

    public async Task<Pagina<StockProducto>> Buscar(string? texto, bool stockBajo, int pagina, int tamano, CancellationToken ct)
    {
        var query = _context.Set<Producto>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var busqueda = texto.Trim().ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(busqueda) || p.Codigo.ToLower().Contains(busqueda));
        }

        if (stockBajo)
            query = query.Where(p => p.Inventarios.Where(i => i.AlmacenId == Almacen.PrincipalId).Sum(i => i.Cantidad) <= p.StockMinimo);

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
