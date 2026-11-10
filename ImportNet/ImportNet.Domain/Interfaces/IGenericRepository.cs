namespace ImportNet.Domain.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<IReadOnlyList<T>> Listar(CancellationToken ct);
    Task<T?> Obtener(int id, CancellationToken ct);
    Task<bool> Existe(int id, CancellationToken ct);
    void Agregar(T entidad);
    void Eliminar(T entidad);
}
