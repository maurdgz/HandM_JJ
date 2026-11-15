using ImportNet.Domain.Entities;
namespace ImportNet.Domain.Interfaces;
public interface ITercerosRepository
{
    Task<IReadOnlyList<Proveedor>> Proveedores(string? texto, CancellationToken ct);
    Task<Proveedor?> Proveedor(int id, CancellationToken ct);
    Task<IReadOnlyList<int>> Productos(int proveedorId, CancellationToken ct);
    Task GuardarProveedor(Proveedor proveedor, IReadOnlyList<int> productos, CancellationToken ct);
    Task EliminarProveedor(int id, CancellationToken ct);
    Task<IReadOnlyList<Cliente>> Clientes(string? texto, CancellationToken ct);
    Task<Cliente?> Cliente(int id, CancellationToken ct);
    Task<bool> DocumentoExiste(string documento, int excluirId, CancellationToken ct);
    void AgregarCliente(Cliente cliente);
    Task EliminarCliente(int id, CancellationToken ct);
}
