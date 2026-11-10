using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IProductoRepository : IGenericRepository<Producto>
{
    Task<bool> ExisteCodigo(string codigo, int excluirId, CancellationToken ct);
    Task<bool> TieneReferencias(int id, CancellationToken ct);
}
