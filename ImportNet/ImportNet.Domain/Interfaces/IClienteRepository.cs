using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IClienteRepository
{
    Task<IReadOnlyList<Cliente>> Clientes(string? texto, CancellationToken ct);
    Task<Cliente?> Cliente(int id, CancellationToken ct);
    Task<bool> DocumentoExiste(string documento, int excluirId, CancellationToken ct);
    void AgregarCliente(Cliente cliente);
    Task EliminarCliente(int id, CancellationToken ct);
    Task<Cliente?> BuscarPorDocumento(string documento, CancellationToken ct);
}
