using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IProveedorRepository
{
    Task<IReadOnlyList<Proveedor>> Proveedores(string? texto, CancellationToken ct);
    Task<Proveedor?> Proveedor(int id, CancellationToken ct);
    Task<IReadOnlyList<int>> Productos(int proveedorId, CancellationToken ct);
    Task GuardarProveedor(Proveedor proveedor, IReadOnlyList<int> productos, CancellationToken ct);
    Task EliminarProveedor(int id, CancellationToken ct);
}
