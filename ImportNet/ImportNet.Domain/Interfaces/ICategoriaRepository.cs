using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface ICategoriaRepository : IGenericRepository<Categoria>
{
    Task<bool> ExisteNombre(string nombreNormalizado, int excluirId, CancellationToken ct);
    Task<bool> TieneProductos(int id, CancellationToken ct);
}
