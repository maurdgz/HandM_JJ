using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface ICategoriaRepository : IGenericRepository<Categoria>
{
    Task<bool> ExisteNombreAsync(string nombreNormalizado, int excluirId, CancellationToken ct = default);
    Task<bool> TieneProductosAsync(int id, CancellationToken ct = default);
}
