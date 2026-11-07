using System.Linq.Expressions;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace ImportNet.Infrastructure.Repositories;

internal sealed class GenericRepository<T>(AppDbContext db) : IGenericRepository<T> where T : CatalogEntity
{
    public async Task<IReadOnlyList<T>> Listar(CancellationToken ct)
        => await db.Set<T>().AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);
    public Task<T?> Obtener(int id, CancellationToken ct) => db.Set<T>().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> Existe(Expression<Func<T, bool>> filtro, CancellationToken ct) => db.Set<T>().AnyAsync(filtro, ct);
    public void Agregar(T entidad) => db.Set<T>().Add(entidad);
    public void Eliminar(T entidad) => db.Set<T>().Remove(entidad);
}

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public IGenericRepository<T> Repository<T>() where T : CatalogEntity => new GenericRepository<T>(db);
    public async Task<bool> ProductoTieneReferencias(int id, CancellationToken ct)
        => await db.Set<Inventario>().AnyAsync(x => x.ProductoId == id, ct)
        || await db.Set<MovimientoInventario>().AnyAsync(x => x.ProductoId == id, ct)
        || await db.Set<DetalleOrdenVenta>().AnyAsync(x => x.ProductoId == id, ct)
        || await db.Set<ProveedorProducto>().AnyAsync(x => x.ProductoId == id, ct);
    public async Task<int> Guardar(CancellationToken ct)
    {
        try { return await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictoException("El registro cambió; recargue e intente de nuevo."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" or "23503" })
        { throw new ConflictoException("El registro está duplicado o tiene referencias; recargue los datos."); }
    }
}
