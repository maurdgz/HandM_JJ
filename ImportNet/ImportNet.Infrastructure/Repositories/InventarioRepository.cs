using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class InventarioRepository(
    AppDbContext context) : IInventarioRepository
{
    public Task<Inventario?> Obtener(
        int productoId,
        int almacenId,
        CancellationToken ct)
    {
        return context.Set<Inventario>()
            .SingleOrDefaultAsync(
                x => x.ProductoId == productoId
                    && x.AlmacenId == almacenId,
                ct);
    }

    public void Agregar(Inventario inventario)
    {
        context.Set<Inventario>().Add(inventario);
    }

    public async Task<Pagina<StockProducto>> Buscar(
        string? texto,
        bool stockBajo,
        int pagina,
        int tamano,
        CancellationToken ct)
    {
        var productos = context.Set<Producto>()
            .AsNoTracking();

        var inventarios = context.Set<Inventario>()
            .AsNoTracking()
            .Where(x => x.AlmacenId == Almacen.PrincipalId);

        var consulta =
            from producto in productos
            join inventario in inventarios
                on producto.Id equals inventario.ProductoId
                into existencias
            from inventario in existencias.DefaultIfEmpty()
            select new
            {
                ProductoId = producto.Id,
                producto.Codigo,
                producto.Nombre,
                producto.UnidadMedida,
                producto.StockMinimo,
                producto.Activo,
                Cantidad = inventario == null
                    ? 0m
                    : inventario.Cantidad
            };

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var busqueda = texto.Trim().ToLowerInvariant();

            consulta = consulta.Where(x =>
                x.Nombre.ToLower().Contains(busqueda)
                || x.Codigo.ToLower().Contains(busqueda));
        }

        if (stockBajo)
        {
            consulta = consulta.Where(x =>
                x.Cantidad <= x.StockMinimo);
        }

        var count = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(x => x.Nombre)
            .ThenBy(x => x.ProductoId)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(x => new StockProducto(
                x.ProductoId,
                x.Codigo,
                x.Nombre,
                x.UnidadMedida,
                Almacen.PrincipalId,
                x.Cantidad,
                x.StockMinimo,
                x.Activo))
            .ToListAsync(ct);

        return new Pagina<StockProducto>(
            items,
            count,
            pagina,
            tamano);
    }
}