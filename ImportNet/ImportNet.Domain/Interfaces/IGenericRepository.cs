using ImportNet.Domain.Common;

namespace ImportNet.Domain.Interfaces;

public interface IGenericRepository<T> where T : Entity
{
    Task<IReadOnlyList<T>> ListarAsync(CancellationToken ct = default);
    Task<T?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteAsync(int id, CancellationToken ct = default);
    void Agregar(T entidad);
    void Actualizar(T entidad);
    void Eliminar(T entidad);
}
