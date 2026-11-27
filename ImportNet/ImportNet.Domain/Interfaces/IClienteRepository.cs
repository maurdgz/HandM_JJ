using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IClienteRepository : IGenericRepository<Cliente>
{
    Task<IReadOnlyList<Cliente>> ListarAsync(string? texto, CancellationToken ct = default);
    Task<Cliente?> BuscarPorDocumentoAsync(string documento, CancellationToken ct = default);
    Task<bool> DocumentoExisteAsync(string documento, int excluirId, CancellationToken ct = default);
    Task EliminarClienteAsync(int id, CancellationToken ct = default);
}
