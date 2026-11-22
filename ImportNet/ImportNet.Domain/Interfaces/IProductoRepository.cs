using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IProductoRepository : IGenericRepository<Producto>
{
    Task<Pagina<Producto>> Buscar(string? texto, int? categoriaId, bool stockBajo, int pagina, int tamano, bool? activo, CancellationToken ct);
    Task<bool> ExisteCodigo(string codigo, int excluirId, CancellationToken ct);
    Task<bool> TieneReferencias(int id, CancellationToken ct);
}
