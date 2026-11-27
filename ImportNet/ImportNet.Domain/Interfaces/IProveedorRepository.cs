using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IProveedorRepository : IGenericRepository<Proveedor>
{
    Task<IReadOnlyList<Proveedor>> ListarAsync(string? texto, CancellationToken ct = default);
    Task<IReadOnlyList<int>> ObtenerProductosIdsAsync(int proveedorId, CancellationToken ct = default);
    Task GuardarConProductosAsync(Proveedor proveedor, IReadOnlyList<int> productos, CancellationToken ct = default);
    Task EliminarProveedorAsync(int id, CancellationToken ct = default);
}
