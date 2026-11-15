using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ImportNet.Infrastructure.Repositories;
internal sealed class TercerosRepository(AppDbContext db) : ITercerosRepository
{
    public async Task<IReadOnlyList<Proveedor>> Proveedores(string? texto, CancellationToken ct) => await db.Set<Proveedor>().AsNoTracking().Where(x => texto == null || x.Nombre.ToLower().Contains(texto.ToLower())).OrderBy(x => x.Nombre).ThenBy(x => x.Id).Take(100).ToListAsync(ct);
    public Task<Proveedor?> Proveedor(int id, CancellationToken ct) => db.Set<Proveedor>().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<int>> Productos(int id, CancellationToken ct) => await db.Set<ProveedorProducto>().Where(x => x.ProveedorId == id).OrderBy(x => x.ProductoId).Select(x => x.ProductoId).ToListAsync(ct);
    public async Task GuardarProveedor(Proveedor p, IReadOnlyList<int> productos, CancellationToken ct)
    {
        if (await db.Set<Producto>().CountAsync(x => productos.Contains(x.Id), ct) != productos.Count) throw new ReglaNegocioException("Producto inexistente o repetido.");
        if (p.Id == 0) db.Add(p);
        var actuales = await db.Set<ProveedorProducto>().Where(x => x.ProveedorId == p.Id).ToListAsync(ct);
        db.RemoveRange(actuales.Where(x => !productos.Contains(x.ProductoId)));
        foreach (var id in productos.Except(actuales.Select(x => x.ProductoId)))
        {
            var relacion = new ProveedorProducto { ProductoId = id };
            p.Suministros.Add(relacion);
        }
    }
    public async Task EliminarProveedor(int id, CancellationToken ct)
    {
        var p = await Proveedor(id, ct) ?? throw new NoEncontradoException("Proveedor no encontrado.");
        if (await db.Set<MovimientoInventario>().AnyAsync(x => x.ProveedorId == id, ct)) throw new ConflictoException("El proveedor tiene historial.");
        db.RemoveRange(await db.Set<ProveedorProducto>().Where(x => x.ProveedorId == id).ToListAsync(ct)); db.Remove(p);
    }
    public async Task<IReadOnlyList<Cliente>> Clientes(string? texto, CancellationToken ct) => await db.Set<Cliente>().AsNoTracking().Where(x => texto == null || x.Nombre.ToLower().Contains(texto.ToLower()) || x.NumeroDocumento.Contains(texto)).OrderBy(x => x.Nombre).ThenBy(x => x.Id).Take(100).ToListAsync(ct);
    public Task<Cliente?> Cliente(int id, CancellationToken ct) => db.Set<Cliente>().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> DocumentoExiste(string documento, int id, CancellationToken ct) => db.Set<Cliente>().AnyAsync(x => x.NumeroDocumento == documento && x.Id != id, ct);
    public void AgregarCliente(Cliente c) => db.Add(c);
    public async Task EliminarCliente(int id, CancellationToken ct)
    {
        var c = await Cliente(id, ct) ?? throw new NoEncontradoException("Cliente no encontrado.");
        if (c.NumeroDocumento == "00000000" || await db.Set<Venta>().AnyAsync(x => x.ClienteId == id, ct)) throw new ConflictoException("Cliente reservado o con ventas.");
        db.Remove(c);
    }
}
