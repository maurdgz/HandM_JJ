using System.Linq.Expressions;
using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

// Deliberately limited to catalog entities: movements and sales do not support generic CRUD.
public interface IGenericRepository<T> where T : CatalogEntity
{
    Task<IReadOnlyList<T>> Listar(CancellationToken ct);
    Task<T?> Obtener(int id, CancellationToken ct);
    Task<bool> Existe(Expression<Func<T, bool>> filtro, CancellationToken ct);
    void Agregar(T entidad);
    void Eliminar(T entidad);
}
public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : CatalogEntity;
    Task<bool> ProductoTieneReferencias(int id, CancellationToken ct);
    Task<int> Guardar(CancellationToken ct);
}
