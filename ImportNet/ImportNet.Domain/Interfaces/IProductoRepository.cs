using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IProductoRepository : IGenericRepository<Producto>
{
    Task<Pagina<Producto>> BuscarAsync(string? texto, int? categoriaId, bool stockBajo, int pagina, int tamano,
        bool? activo, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, Producto>> ObtenerPorIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<bool> ExisteCodigoAsync(string codigo, int excluirId, CancellationToken ct = default);
    Task<bool> TieneReferenciasAsync(int id, CancellationToken ct = default);
}
